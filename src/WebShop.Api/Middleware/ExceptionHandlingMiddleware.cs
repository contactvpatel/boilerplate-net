using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebShop.Api.Models;

namespace WebShop.Api.Middleware;

/// <summary>
/// Middleware for handling exceptions globally and returning standardized error responses.
/// Uses ExceptionHandlingStrategy for exception-to-response mapping (strategy pattern).
/// </summary>
public class ExceptionHandlingMiddleware(
    ExceptionHandlingOptions options,
    ExceptionHandlingStrategy strategy,
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>
    /// Structured logging template following the project's logging guidelines.
    /// </summary>
    private const string LogTemplate = "Area: {Area}, RequestPath: {RequestPath}, RequestMethod: {RequestMethod}, ErrorId: {ErrorId}, Message: {Message}, InnerException: {InnerException}";

    /// <summary>
    /// Invokes the middleware to handle exceptions.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            (HttpStatusCode statusCode, string? message, LogLevel logLevel) = strategy.GetHandling(ex);
            await ProcessExceptionAsync(context, ex, statusCode, message, logLevel, "ExceptionHandlingMiddleware.InvokeAsync");
        }
    }

    /// <summary>
    /// Processes the exception and generates a standardized error response.
    /// </summary>
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

        options.AddResponseDetails?.Invoke(context, exception, errorResponse);

        string innerExMessage = GetInnermostExceptionMessage(exception);
        LogLevel level = options.DetermineLogLevel?.Invoke(exception) ?? defaultLogLevel;

        if (string.IsNullOrEmpty(exception.Data["ErrorId"]?.ToString()))
        {
            exception.Data["ErrorId"] = errorId;
        }

        // Use structured logging template (no string interpolation)
        logger.Log(
            level,
            exception,
            LogTemplate,
            area,
            context.Request.Path,
            context.Request.Method,
            errorId,
            finalMessage,
            innerExMessage ?? string.Empty);

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

