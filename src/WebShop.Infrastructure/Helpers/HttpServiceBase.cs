using System.Buffers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Base class for HTTP service implementations that provides common functionality
/// for making HTTP requests, error handling, and response deserialization.
/// </summary>
/// <remarks>
/// SOLID: S — transport + response parsing; O — extend via HttpClientName, CreateHttpClient; L — subclasses substitutable; D — depends on IHttpClientFactory, ILogger, IOptions.
/// DRY: Error handling is centralized in ExecuteWithErrorHandlingAsync; success/size checks in EnsureSuccessAndValidateResponseSizeAsync; JSON read in ReadResponseJsonAsync/ReadFromJsonStreamAsync.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="HttpServiceBase"/> class.
/// </remarks>
/// <param name="httpClientFactory">The HTTP client factory.</param>
/// <param name="logger">The logger.</param>
/// <param name="resilienceOptions">The HTTP resilience configuration options.</param>
public abstract class HttpServiceBase(
    IHttpClientFactory httpClientFactory,
    ILogger logger,
    IOptions<HttpResilienceOptions> resilienceOptions)
{
    protected readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    protected readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly JsonSerializerOptions _jsonOptions = JsonContext.Default.Options;
    protected readonly HttpResilienceOptions _resilienceOptions = resilienceOptions?.Value ?? throw new ArgumentNullException(nameof(resilienceOptions));

    /// <summary>
    /// Gets the name of the HTTP client to use from the factory.
    /// </summary>
    protected abstract string HttpClientName { get; }

    /// <summary>
    /// Validates that the endpoint is not null or whitespace. Throws <see cref="ArgumentException"/> if invalid.
    /// </summary>
    private static void ValidateEndpoint(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint cannot be null or whitespace.", nameof(endpoint));
        }
    }

    /// <summary>
    /// Executes an async operation and, on exception, logs and re-throws via <see cref="HttpErrorHandler.LogAndThrowException"/>.
    /// Keeps error-handling logic in one place (DRY) and preserves the same behavior for all HTTP operations.
    /// </summary>
    private async Task<T> ExecuteWithErrorHandlingAsync<T>(string endpoint, string operation, Func<Task<T>> operationAsync)
    {
        try
        {
            return await operationAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            HttpErrorHandler.LogAndThrowException(ex, _logger, endpoint, operation);
            throw; // Unreachable; LogAndThrowException always throws
        }
    }

    /// <summary>
    /// Ensures the response is successful and body size is within limits. Throws on non-success or oversized response.
    /// </summary>
    private async Task EnsureSuccessAndValidateResponseSizeAsync(
        HttpResponseMessage response,
        string endpoint,
        string operation,
        CancellationToken cancellationToken)
    {
        await HttpErrorHandler.HandleResponseAndThrowAsync(response, _logger, endpoint, operation, cancellationToken).ConfigureAwait(false);

        long? contentLength = response.Content.Headers.ContentLength;
        if (contentLength.HasValue && contentLength.Value > _resilienceOptions.MaxResponseSizeBytes)
        {
            throw new HttpRequestException(
                $"Response body size ({contentLength} bytes) exceeds maximum allowed size ({_resilienceOptions.MaxResponseSizeBytes} bytes).");
        }
    }

    /// <summary>
    /// Deserializes response JSON using either a fast path (no copy) or a bounded stream path.
    /// Fast path: when Content-Length is present and within limit, deserialize directly from content (no extra copy or MemoryStream).
    /// Bounded path: when Content-Length is missing (e.g. chunked encoding), copy through a size-limited stream so we enforce MaxResponseSizeBytes.
    /// </summary>
    private async Task<T?> ReadResponseJsonAsync<T>(HttpContent content, CancellationToken cancellationToken)
    {
        long? contentLength = content.Headers.ContentLength;

        // Fast path: Content-Length present and within limit — deserialize directly from the response stream.
        // Avoids the extra copy and MemoryStream allocation that ReadFromJsonStreamAsync does; used for most responses.
        if (contentLength.HasValue && contentLength.Value > 0 && contentLength.Value <= _resilienceOptions.MaxResponseSizeBytes)
        {
            return await content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        }

        // Bounded path: Content-Length missing (chunked) or zero — enforce or handle via size-limited stream read.
        // Ensures we never read more than MaxResponseSizeBytes when the server doesn't send a reliable Content-Length.
        return await ReadFromJsonStreamAsync<T>(content, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads response content via stream and deserializes to JSON while enforcing MaxResponseSizeBytes.
    /// Used when Content-Length is missing (chunked encoding) so we can cap how much we read.
    /// </summary>
    private async Task<T?> ReadFromJsonStreamAsync<T>(HttpContent content, CancellationToken cancellationToken)
    {
        await using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        int maxBytes = _resilienceOptions.MaxResponseSizeBytes;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Min(81920, maxBytes)); // 80KB or max, whichever is smaller
        try
        {
            using MemoryStream memoryStream = new(capacity: Math.Min(maxBytes, 65536));
            int totalRead = 0;
            int bytesRead;
            int chunkSize = Math.Min(buffer.Length, maxBytes);

            while (totalRead < maxBytes &&
                   (bytesRead = await stream.ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken).ConfigureAwait(false)) > 0)
            {
                memoryStream.Write(buffer, 0, bytesRead);
                totalRead += bytesRead;
                chunkSize = Math.Min(buffer.Length, maxBytes - totalRead);
            }

            if (totalRead >= maxBytes && await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false) > 0)
            {
                throw new HttpRequestException(
                    $"Response body size exceeds maximum allowed size ({_resilienceOptions.MaxResponseSizeBytes} bytes).");
            }

            if (memoryStream.Length == 0)
            {
                return default;
            }

            memoryStream.Position = 0;
            return await JsonSerializer.DeserializeAsync<T>(memoryStream, _jsonOptions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Executes a GET request and deserializes the response.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="endpoint">The endpoint path (relative to base URL).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    protected async Task<T?> GetAsync<T>(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<T>(endpoint, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a GET request and deserializes the response with per-request configuration.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="endpoint">The endpoint path (relative to base URL).</param>
    /// <param name="configureRequest">Optional action to configure the HttpRequestMessage (e.g., set Bearer token, headers).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    protected async Task<T?> GetAsync<T>(
        string endpoint,
        Action<HttpRequestMessage>? configureRequest,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandlingAsync(endpoint, "GET", async () =>
        {
            ValidateEndpoint(endpoint);
            HttpClient httpClient = CreateHttpClient();
            using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
            configureRequest?.Invoke(request);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessAndValidateResponseSizeAsync(response, endpoint, "GET", cancellationToken).ConfigureAwait(false);

            if (response.Content.Headers.ContentLength == 0)
            {
                _logger.LogDebug("Empty response body from {Endpoint}", endpoint);
                return default;
            }

            T? result = await ReadResponseJsonAsync<T>(response.Content, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Successfully fetched data from {Endpoint}", endpoint);
            return result;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a GET request and deserializes the response as a collection.
    /// </summary>
    /// <typeparam name="T">The type to deserialize each item to.</typeparam>
    /// <param name="endpoint">The endpoint path (relative to base URL).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized collection as a materialized list.</returns>
    protected async Task<IReadOnlyList<T>> GetCollectionAsync<T>(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        return await GetCollectionAsync<T>(endpoint, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a GET request and deserializes the response as a collection with per-request configuration.
    /// </summary>
    /// <typeparam name="T">The type to deserialize each item to.</typeparam>
    /// <param name="endpoint">The endpoint path (relative to base URL).</param>
    /// <param name="configureRequest">Optional action to configure the HttpRequestMessage (e.g., set Bearer token, headers).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized collection as a materialized list.</returns>
    protected async Task<IReadOnlyList<T>> GetCollectionAsync<T>(
        string endpoint,
        Action<HttpRequestMessage>? configureRequest,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandlingAsync(endpoint, "GET", async () =>
        {
            ValidateEndpoint(endpoint);
            HttpClient httpClient = CreateHttpClient();
            using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
            configureRequest?.Invoke(request);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessAndValidateResponseSizeAsync(response, endpoint, "GET", cancellationToken).ConfigureAwait(false);

            if (response.Content.Headers.ContentLength == 0)
            {
                _logger.LogDebug("Empty response body from {Endpoint}", endpoint);
                return [];
            }

            IEnumerable<T>? result = await ReadResponseJsonAsync<IEnumerable<T>>(response.Content, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<T> materialized = result?.ToList() ?? [];
            _logger.LogDebug("Successfully fetched {Count} items from {Endpoint}", materialized.Count, endpoint);
            return materialized;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a POST request with a JSON body and deserializes the response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
    /// <param name="endpoint">The endpoint path (relative to base URL).</param>
    /// <param name="request">The request object to serialize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    protected async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<TRequest, TResponse>(endpoint, request, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a POST request with a JSON body and deserializes the response with per-request configuration.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response to.</typeparam>
    /// <param name="endpoint">The endpoint path (relative to base URL).</param>
    /// <param name="request">The request object to serialize.</param>
    /// <param name="configureRequest">Optional action to configure the HttpRequestMessage (e.g., set Bearer token, headers).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    protected async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        Action<HttpRequestMessage>? configureRequest,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandlingAsync(endpoint, "POST", async () =>
        {
            ValidateEndpoint(endpoint);
            ArgumentNullException.ThrowIfNull(request);

            HttpClient httpClient = CreateHttpClient();
            string jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            int contentSize = Encoding.UTF8.GetByteCount(jsonContent);
            if (contentSize > _resilienceOptions.MaxRequestSizeBytes)
            {
                throw new ArgumentException(
                    $"Request body size ({contentSize} bytes) exceeds maximum allowed size ({_resilienceOptions.MaxRequestSizeBytes} bytes).",
                    nameof(request));
            }

            using StringContent content = new(jsonContent, Encoding.UTF8, "application/json");
            using HttpRequestMessage httpRequest = new(HttpMethod.Post, endpoint) { Content = content };
            configureRequest?.Invoke(httpRequest);

            using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessAndValidateResponseSizeAsync(response, endpoint, "POST", cancellationToken).ConfigureAwait(false);

            if (response.Content.Headers.ContentLength == 0)
            {
                _logger.LogDebug("Empty response body from {Endpoint}", endpoint);
                return default;
            }

            TResponse? result = await ReadResponseJsonAsync<TResponse>(response.Content, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Successfully posted to {Endpoint}", endpoint);
            return result;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a POST request with a JSON body and returns a boolean indicating success.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <param name="endpoint">The endpoint path (relative to base URL).</param>
    /// <param name="request">The request object to serialize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the request was successful.</returns>
    protected async Task<bool> PostAsync<TRequest>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync(endpoint, request, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a POST request with a JSON body and returns a boolean indicating success with per-request configuration.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <param name="endpoint">The endpoint path (relative to base URL).</param>
    /// <param name="request">The request object to serialize.</param>
    /// <param name="configureRequest">Optional action to configure the HttpRequestMessage (e.g., set Bearer token, headers).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the request was successful.</returns>
    protected async Task<bool> PostAsync<TRequest>(
        string endpoint,
        TRequest request,
        Action<HttpRequestMessage>? configureRequest,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandlingAsync(endpoint, "POST", async () =>
        {
            ValidateEndpoint(endpoint);
            ArgumentNullException.ThrowIfNull(request);

            HttpClient httpClient = CreateHttpClient();
            string jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            int contentSize = Encoding.UTF8.GetByteCount(jsonContent);
            if (contentSize > _resilienceOptions.MaxRequestSizeBytes)
            {
                throw new ArgumentException(
                    $"Request body size ({contentSize} bytes) exceeds maximum allowed size ({_resilienceOptions.MaxRequestSizeBytes} bytes).",
                    nameof(request));
            }

            using StringContent content = new(jsonContent, Encoding.UTF8, "application/json");
            using HttpRequestMessage httpRequest = new(HttpMethod.Post, endpoint) { Content = content };
            configureRequest?.Invoke(httpRequest);

            using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            await HttpErrorHandler.HandleResponseAndThrowAsync(response, _logger, endpoint, "POST", cancellationToken).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a POST request without a body and returns a boolean indicating success.
    /// </summary>
    /// <param name="endpoint">The endpoint path (relative to base URL).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the request was successful.</returns>
    protected async Task<bool> PostAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync(endpoint, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a POST request without a body and returns a boolean indicating success with per-request configuration.
    /// </summary>
    /// <param name="endpoint">The endpoint path (relative to base URL).</param>
    /// <param name="configureRequest">Optional action to configure the HttpRequestMessage (e.g., set Bearer token, headers).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the request was successful.</returns>
    protected async Task<bool> PostAsync(
        string endpoint,
        Action<HttpRequestMessage>? configureRequest,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandlingAsync(endpoint, "POST", async () =>
        {
            ValidateEndpoint(endpoint);
            HttpClient httpClient = CreateHttpClient();
            using HttpRequestMessage request = new(HttpMethod.Post, endpoint);
            configureRequest?.Invoke(request);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await HttpErrorHandler.HandleResponseAndThrowAsync(response, _logger, endpoint, "POST", cancellationToken).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates and configures an HttpClient instance.
    /// Override this method to add service-specific configuration (headers, authentication, etc.).
    /// </summary>
    /// <returns>Configured HttpClient instance.</returns>
    protected virtual HttpClient CreateHttpClient()
    {
        return _httpClientFactory.CreateClient(HttpClientName);
    }
}

