using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebShop.Api.Models;

namespace WebShop.Api.Middleware;

/// <summary>
/// Middleware for handling exceptions globally and returning standardized error responses.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
/// </remarks>
/// <param name="options">Exception handling options.</param>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">Logger instance.</param>
public class ExceptionHandlingMiddleware(
    ExceptionHandlingOptions options,
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ExceptionHandlingOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Structured logging template following the project's logging guidelines.
    /// Format: Area: {Area}, RequestPath: {RequestPath}, RequestMethod: {RequestMethod}, ErrorId: {ErrorId}, Message: {Message}
    /// </summary>
    private const string LogTemplate = "Area: {Area}, RequestPath: {RequestPath}, RequestMethod: {RequestMethod}, ErrorId: {ErrorId}, Message: {Message}";

    /// <summary>
    /// Invokes the middleware to handle exceptions.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException ex)
        {
            await HandleOperationCanceledExceptionAsync(context, ex);
        }
        catch (ArgumentException ex) // Catches both ArgumentException and ArgumentNullException
        {
            await HandleArgumentExceptionAsync(context, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleUnauthorizedAccessExceptionAsync(context, ex);
        }
        catch (KeyNotFoundException ex)
        {
            await HandleKeyNotFoundExceptionAsync(context, ex);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            await HandleNotFoundExceptionAsync(context, ex);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == 413)
        {
            await HandlePayloadTooLargeExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles general exceptions.
    /// </summary>
    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode statusCode = exception.GetType() == typeof(UnauthorizedAccessException)
            ? HttpStatusCode.Forbidden
            : HttpStatusCode.InternalServerError;

        string message = exception.GetType() == typeof(ApplicationException) ||
                         exception.GetType() == typeof(UnauthorizedAccessException)
            ? exception.Message
            : null!; // Will be generated in ProcessExceptionAsync

        return ProcessExceptionAsync(
            context,
            exception,
            statusCode,
            message,
            LogLevel.Error,
            "ExceptionHandlingMiddleware.HandleExceptionAsync");
    }

    /// <summary>
    /// Handles operation canceled exceptions (request cancellation/timeout).
    /// </summary>
    private Task HandleOperationCanceledExceptionAsync(HttpContext context, Exception exception)
    {
        return ProcessExceptionAsync(
            context,
            exception,
            (HttpStatusCode)499, // Client Closed Request (Non-Standard)
            "Request was canceled by the client.",
            LogLevel.Information,
            "ExceptionHandlingMiddleware.HandleOperationCanceledExceptionAsync");
    }

    /// <summary>
    /// Handles argument exceptions (bad request).
    /// </summary>
    private Task HandleArgumentExceptionAsync(HttpContext context, Exception exception)
    {
        return ProcessExceptionAsync(
            context,
            exception,
            HttpStatusCode.BadRequest,
            exception.Message,
            LogLevel.Warning,
            "ExceptionHandlingMiddleware.HandleArgumentExceptionAsync");
    }

    /// <summary>
    /// Handles unauthorized access exceptions.
    /// </summary>
    private Task HandleUnauthorizedAccessExceptionAsync(HttpContext context, Exception exception)
    {
        return ProcessExceptionAsync(
            context,
            exception,
            HttpStatusCode.Forbidden,
            exception.Message,
            LogLevel.Warning,
            "ExceptionHandlingMiddleware.HandleUnauthorizedAccessExceptionAsync");
    }

    /// <summary>
    /// Handles key not found exceptions.
    /// </summary>
    private Task HandleKeyNotFoundExceptionAsync(HttpContext context, Exception exception)
    {
        return ProcessExceptionAsync(
            context,
            exception,
            HttpStatusCode.NotFound,
            exception.Message,
            LogLevel.Information,
            "ExceptionHandlingMiddleware.HandleKeyNotFoundExceptionAsync");
    }

    /// <summary>
    /// Handles not found exceptions (InvalidOperationException with "not found" message).
    /// </summary>
    private Task HandleNotFoundExceptionAsync(HttpContext context, Exception exception)
    {
        return ProcessExceptionAsync(
            context,
            exception,
            HttpStatusCode.NotFound,
            exception.Message,
            LogLevel.Information,
            "ExceptionHandlingMiddleware.HandleNotFoundExceptionAsync");
    }

    /// <summary>
    /// Handles payload too large exceptions (BadHttpRequestException with 413 status).
    /// This occurs when Kestrel rejects requests exceeding MaxRequestBodySize or other size limits.
    /// </summary>
    private Task HandlePayloadTooLargeExceptionAsync(HttpContext context, Exception exception)
    {
        // Extract the actual limit from the exception message if available
        string message = exception.Message.Contains("Request body too large", StringComparison.OrdinalIgnoreCase) ||
                        exception.Message.Contains("exceeds", StringComparison.OrdinalIgnoreCase)
            ? exception.Message
            : "Request body size exceeds the maximum allowed limit. Please reduce the request size and try again.";

        return ProcessExceptionAsync(
            context,
            exception,
            HttpStatusCode.RequestEntityTooLarge, // 413
            message,
            LogLevel.Warning,
            "ExceptionHandlingMiddleware.HandlePayloadTooLargeExceptionAsync");
    }

    /// <summary>
    /// Common method to process exceptions and generate standardized error responses.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="errorMessage">The error message. If null, a generic message with ErrorId will be generated.</param>
    /// <param name="defaultLogLevel">The default log level if not determined by options.</param>
    /// <param name="area">The area/method name for structured logging.</param>
    private Task ProcessExceptionAsync(
        HttpContext context,
        Exception exception,
        HttpStatusCode statusCode,
        string? errorMessage,
        LogLevel defaultLogLevel,
        string area)
    {
        string errorId = GetOrCreateErrorId(exception);

        // Generate error message if not provided
        string finalMessage = errorMessage ??
            $"Error occurred in the API. Please use the ErrorId [{errorId}] and contact support team if the problem persists.";

        List<ApiError> apiErrors = new()
        {
            new ApiError
            {
                ErrorId = errorId,
                StatusCode = (short)statusCode,
                Message = finalMessage
            }
        };

        Response<ApiError> errorResponse = new(null)
        {
            Succeeded = false,
            Errors = apiErrors
        };

        _options.AddResponseDetails?.Invoke(context, exception, errorResponse);

        string innerExMessage = GetInnermostExceptionMessage(exception);
        LogLevel level = _options.DetermineLogLevel?.Invoke(exception) ?? defaultLogLevel;

        if (string.IsNullOrEmpty(exception.Data["ErrorId"]?.ToString()))
        {
            exception.Data["ErrorId"] = errorId;
        }

        // Build log message with inner exception details
        string logMessage = string.IsNullOrWhiteSpace(innerExMessage)
            ? finalMessage
            : $"{finalMessage} Inner exception: {innerExMessage}";

        // Use structured logging template following project guidelines
        _logger.Log(
            level,
            exception,
            LogTemplate,
            area,
            context.Request.Path,
            context.Request.Method,
            errorId,
            logMessage);

        return WriteErrorResponseAsync(context, errorResponse, statusCode);
    }

    /// <summary>
    /// Gets or creates an error ID for the exception.
    /// </summary>
    private static string GetOrCreateErrorId(Exception exception)
    {
        return !string.IsNullOrEmpty(exception.Data["ErrorId"]?.ToString())
            ? exception.Data["ErrorId"]!.ToString()!
            : Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Gets the innermost exception message.
    /// </summary>
    private static string GetInnermostExceptionMessage(Exception exception)
    {
        Exception current = exception;
        while (current.InnerException != null)
        {
            current = current.InnerException;
        }
        return current.Message;
    }

    /// <summary>
    /// Writes the error response to the HTTP context.
    /// </summary>
    private static Task WriteErrorResponseAsync(
        HttpContext context,
        Response<ApiError> errorResponse,
        HttpStatusCode statusCode)
    {
        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return context.Response.WriteAsJsonAsync(errorResponse, jsonOptions, context.RequestAborted);
    }
}

