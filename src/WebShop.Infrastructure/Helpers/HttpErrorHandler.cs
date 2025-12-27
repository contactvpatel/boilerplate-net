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
    /// <exception cref="HttpRequestException">Thrown when the response is not successful.</exception>
    /// <summary>
    /// Maximum size of error content to read (10KB) to prevent DoS attacks.
    /// </summary>
    private const int MaxErrorContentSize = 10 * 1024; // 10KB

    public static async Task HandleResponseAndThrowAsync(
        HttpResponseMessage response,
        ILogger logger,
        string endpoint,
        string operation)
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
                using Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using MemoryStream memoryStream = new();

                // Use ArrayPool for temporary buffer to reduce allocations
                byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(4096);
                int totalBytesRead = 0;
                try
                {
                    int bytesRead;

                    while (totalBytesRead < MaxErrorContentSize &&
                           (bytesRead = await contentStream.ReadAsync(
                               buffer, 0, Math.Min(buffer.Length, MaxErrorContentSize - totalBytesRead)).ConfigureAwait(false)) > 0)
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
                errorContent = await reader.ReadToEndAsync().ConfigureAwait(false);

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

        // Security: Sanitize error content to remove sensitive data before logging
        string sanitizedErrorContent = SensitiveDataSanitizer.Sanitize(errorContent);

        switch (response.StatusCode)
        {
            case HttpStatusCode.BadRequest:
                logger.LogWarning("{Message}. Response: {ErrorContent}", logMessage, sanitizedErrorContent);
                throw new HttpRequestException(
                    $"Bad request: {logMessage}. Response: {sanitizedErrorContent}", null, response.StatusCode);

            case HttpStatusCode.Unauthorized:
                logger.LogWarning("{Message}. Unauthorized access.", logMessage);
                throw new HttpRequestException(
                    $"Unauthorized: {logMessage}", null, response.StatusCode);

            case HttpStatusCode.Forbidden:
                logger.LogWarning("{Message}. Forbidden access.", logMessage);
                throw new HttpRequestException(
                    $"Forbidden: {logMessage}", null, response.StatusCode);

            case HttpStatusCode.NotFound:
                logger.LogInformation("{Message}. Resource not found.", logMessage);
                throw new HttpRequestException(
                    $"Not found: {logMessage}", null, response.StatusCode);

            case HttpStatusCode.TooManyRequests:
                logger.LogWarning("{Message}. Rate limit exceeded.", logMessage);
                throw new HttpRequestException(
                    $"Rate limit exceeded: {logMessage}", null, response.StatusCode);

            case HttpStatusCode.InternalServerError:
            case HttpStatusCode.BadGateway:
            case HttpStatusCode.ServiceUnavailable:
            case HttpStatusCode.GatewayTimeout:
                logger.LogError("{Message}. Server error. Response: {ErrorContent}", logMessage, sanitizedErrorContent);
                throw new HttpRequestException(
                    $"Server error: {logMessage}. Response: {sanitizedErrorContent}", null, response.StatusCode);

            default:
                logger.LogWarning("{Message}. Response: {ErrorContent}", logMessage, sanitizedErrorContent);
                throw new HttpRequestException(
                    $"HTTP error: {logMessage}. Response: {sanitizedErrorContent}", null, response.StatusCode);
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
                    $"HTTP request failed during {operation} to {endpoint}: {sanitizedHttpMessage}", httpEx);

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

