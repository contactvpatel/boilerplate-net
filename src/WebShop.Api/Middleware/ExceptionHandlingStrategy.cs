using System.Net;

namespace WebShop.Api.Middleware;

/// <summary>
/// Encapsulates exception-to-HTTP-handling mapping (OCP: open for extension via new handlers).
/// </summary>
public sealed class ExceptionHandlingStrategy
{
    /// <summary>
    /// Gets the HTTP status code, error message, and log level for the given exception.
    /// </summary>
    public (HttpStatusCode StatusCode, string? Message, LogLevel LogLevel) GetHandling(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => ((HttpStatusCode)499, "Request was canceled by the client.", LogLevel.Information),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message, LogLevel.Warning),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, exception.Message, LogLevel.Warning),
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message, LogLevel.Information),
            InvalidOperationException when exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.NotFound, exception.Message, LogLevel.Information),
            BadHttpRequestException { StatusCode: 413 } ex => (HttpStatusCode.RequestEntityTooLarge, GetPayloadTooLargeMessage(ex), LogLevel.Warning),
            _ => GetDefaultHandling(exception)
        };
    }

    private static string GetPayloadTooLargeMessage(BadHttpRequestException ex)
    {
        return ex.Message.Contains("Request body too large", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("exceeds", StringComparison.OrdinalIgnoreCase)
            ? ex.Message
            : "Request body size exceeds the maximum allowed limit. Please reduce the request size and try again.";
    }

    private static (HttpStatusCode StatusCode, string? Message, LogLevel LogLevel) GetDefaultHandling(Exception exception)
    {
        HttpStatusCode statusCode = exception is UnauthorizedAccessException
            ? HttpStatusCode.Forbidden
            : HttpStatusCode.InternalServerError;

        string? message = exception is ApplicationException or UnauthorizedAccessException
            ? exception.Message
            : null;

        return (statusCode, message, LogLevel.Error);
    }
}
