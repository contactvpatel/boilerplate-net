using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Centralized error handling for HTTP operations.
/// </summary>
public static class HttpErrorHandler
{
    /// <summary>
    /// Handles HTTP response errors, logs them, and throws an exception if the response is not successful.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="endpoint">The endpoint that was called.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="HttpRequestException">Thrown when the response is not successful.</exception>
    /// <summary>
    /// Maximum size of error content to read (10KB) to prevent DoS attacks.
    /// </summary>
    private const int MaxErrorContentSize = 10 * 1024; // 10KB

    public static async Task HandleResponseAndThrowAsync(
        HttpResponseMessage response,
        ILogger logger,
        string endpoint,
        string operation,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? errorContent = null;
        try
        {
            // Security: Limit error content size to prevent DoS attacks
            long? contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > MaxErrorContentSize)
            {
                errorContent = $"[Error content too large: {contentLength} bytes, limit: {MaxErrorContentSize} bytes]";
                logger.LogWarning("Error response content exceeds maximum size limit: {ContentLength} bytes", contentLength);
            }
            else
            {
                // Read content with size limit
                using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using MemoryStream memoryStream = new();

                // Use ArrayPool for temporary buffer to reduce allocations
                byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(4096);
                int totalBytesRead = 0;
                try
                {
                    int bytesRead;

                    while (totalBytesRead < MaxErrorContentSize &&
                           (bytesRead = await contentStream.ReadAsync(
                               buffer.AsMemory(0, Math.Min(buffer.Length, MaxErrorContentSize - totalBytesRead)),
                               cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        totalBytesRead += bytesRead;
                        memoryStream.Write(buffer, 0, bytesRead);
                    }
                }
                finally
                {
                    // Return buffer to pool for reuse
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                }

                memoryStream.Position = 0;
                using StreamReader reader = new(memoryStream, System.Text.Encoding.UTF8);
                errorContent = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

                if (totalBytesRead >= MaxErrorContentSize)
                {
                    errorContent += $" [Truncated at {MaxErrorContentSize} bytes]";
                }
            }
        }
        catch
        {
            // Ignore errors reading error content
        }

        string logMessage = $"{operation} failed for {endpoint}. Status: {response.StatusCode}";
        string sanitizedErrorContent = SensitiveDataSanitizer.Sanitize(errorContent);

        string exceptionMessage = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => $"Bad request: {logMessage}. Response: {sanitizedErrorContent}",
            HttpStatusCode.Unauthorized => $"Unauthorized: {logMessage}",
            HttpStatusCode.Forbidden => $"Forbidden: {logMessage}",
            HttpStatusCode.NotFound => $"Not found: {logMessage}",
            HttpStatusCode.TooManyRequests => $"Rate limit exceeded: {logMessage}",
            HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout
                => $"Server error: {logMessage}. Response: {sanitizedErrorContent}",
            _ => $"HTTP error: {logMessage}. Response: {sanitizedErrorContent}"
        };

        LogFailedResponse(logger, response.StatusCode, logMessage, sanitizedErrorContent);
        throw new HttpRequestException(exceptionMessage, null, response.StatusCode);
    }

    /// <summary>
    /// Logs a failed HTTP response with the appropriate level and message for the status code.
    /// </summary>
    private static void LogFailedResponse(ILogger logger, HttpStatusCode statusCode, string logMessage, string sanitizedErrorContent)
    {
        switch (statusCode)
        {
            case HttpStatusCode.NotFound:
                logger.LogInformation("{Message}. Resource not found.", logMessage);
                break;
            case HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout:
                logger.LogError("{Message}. Server error. Response: {ErrorContent}", logMessage, sanitizedErrorContent);
                break;
            case HttpStatusCode.Unauthorized:
                logger.LogWarning("{Message}. Unauthorized access.", logMessage);
                break;
            case HttpStatusCode.Forbidden:
                logger.LogWarning("{Message}. Forbidden access.", logMessage);
                break;
            case HttpStatusCode.TooManyRequests:
                logger.LogWarning("{Message}. Rate limit exceeded.", logMessage);
                break;
            default:
                logger.LogWarning("{Message}. Response: {ErrorContent}", logMessage, sanitizedErrorContent);
                break;
        }
    }

    /// <summary>
    /// Logs exceptions during HTTP operations and re-throws them.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="endpoint">The endpoint that was called.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <exception cref="HttpRequestException">Re-thrown for HTTP-related exceptions.</exception>
    /// <exception cref="TaskCanceledException">Re-thrown for timeout/cancellation exceptions.</exception>
    /// <exception cref="JsonException">Re-thrown for JSON deserialization exceptions.</exception>
    public static void LogAndThrowException(
        Exception exception,
        ILogger logger,
        string endpoint,
        string operation)
    {
        string logMessage = $"{operation} failed for {endpoint}";

        switch (exception)
        {
            case HttpRequestException httpEx:
                // Security: Sanitize exception message to remove sensitive data
                string sanitizedHttpMessage = SensitiveDataSanitizer.Sanitize(httpEx.Message);
                logger.LogError(httpEx, "{Message}. HTTP request exception: {HttpMessage}", logMessage, sanitizedHttpMessage);
                throw new HttpRequestException(
                    $"HTTP request failed during {operation} to {endpoint}: {sanitizedHttpMessage}", httpEx, httpEx.StatusCode);

            case TaskCanceledException taskEx:
                logger.LogWarning(taskEx, "{Message}. Request timeout or cancellation.", logMessage);
                throw new TaskCanceledException(
                    $"Request timeout or cancellation during {operation} to {endpoint}: {taskEx.Message}", taskEx);

            case JsonException jsonEx:
                // Security: Sanitize exception message to remove sensitive data
                string sanitizedJsonMessage = SensitiveDataSanitizer.Sanitize(jsonEx.Message);
                logger.LogError(jsonEx, "{Message}. JSON deserialization error: {JsonMessage}", logMessage, sanitizedJsonMessage);
                throw new JsonException(
                    $"JSON deserialization error during {operation} to {endpoint}: {sanitizedJsonMessage}",
                    jsonEx.Path,
                    jsonEx.LineNumber,
                    jsonEx.BytePositionInLine,
                    jsonEx);

            default:
                // Security: Sanitize exception message to remove sensitive data
                string sanitizedExceptionMessage = SensitiveDataSanitizer.SanitizeException(exception);
                logger.LogError(exception, "{Message}. Unexpected error: {ErrorMessage}", logMessage, sanitizedExceptionMessage);
                throw new HttpRequestException(
                    $"Unexpected error during {operation} to {endpoint}: {sanitizedExceptionMessage}", exception);
        }
    }
}
