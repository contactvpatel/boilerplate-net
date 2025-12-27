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
public abstract class HttpServiceBase
{
    protected readonly IHttpClientFactory _httpClientFactory;
    protected readonly ILogger _logger;
    protected readonly JsonSerializerOptions _jsonOptions;
    protected readonly HttpResilienceOptions _resilienceOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServiceBase"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="resilienceOptions">The HTTP resilience configuration options.</param>
    protected HttpServiceBase(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        IOptions<HttpResilienceOptions> resilienceOptions)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resilienceOptions = resilienceOptions?.Value ?? throw new ArgumentNullException(nameof(resilienceOptions));

        // Use source generator context for better performance
        // JsonContext.Default.Options includes PropertyNameCaseInsensitive = true via JsonSourceGenerationOptions
        _jsonOptions = JsonContext.Default.Options;
    }

    /// <summary>
    /// Gets the name of the HTTP client to use from the factory.
    /// </summary>
    protected abstract string HttpClientName { get; }

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
        HttpClient httpClient = CreateHttpClient();

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
            configureRequest?.Invoke(request);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Throw exception if response is not successful
            await HttpErrorHandler.HandleResponseAndThrowAsync(response, _logger, endpoint, "GET").ConfigureAwait(false);

            // Security: Validate response size before reading to prevent DoS attacks
            long? contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > _resilienceOptions.MaxResponseSizeBytes)
            {
                throw new HttpRequestException(
                    $"Response body size ({contentLength} bytes) exceeds maximum allowed size ({_resilienceOptions.MaxResponseSizeBytes} bytes).");
            }

            // Deserialize response
            T? result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Successfully fetched data from {Endpoint}", endpoint);
            return result;
        }
        catch (Exception ex)
        {
            // Log and re-throw the exception instead of swallowing it
            HttpErrorHandler.LogAndThrowException(ex, _logger, endpoint, "GET");
            throw; // Unreachable, but compiler requires it
        }
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
        HttpClient httpClient = CreateHttpClient();

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
            configureRequest?.Invoke(request);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Throw exception if response is not successful
            await HttpErrorHandler.HandleResponseAndThrowAsync(response, _logger, endpoint, "GET").ConfigureAwait(false);

            // Security: Validate response size before reading to prevent DoS attacks
            long? contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > _resilienceOptions.MaxResponseSizeBytes)
            {
                throw new HttpRequestException(
                    $"Response body size ({contentLength} bytes) exceeds maximum allowed size ({_resilienceOptions.MaxResponseSizeBytes} bytes).");
            }

            // Deserialize response and materialize immediately to avoid multiple enumerations
            IEnumerable<T>? result = await response.Content.ReadFromJsonAsync<IEnumerable<T>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<T> materialized = result?.ToList() ?? [];
            _logger.LogDebug("Successfully fetched {Count} items from {Endpoint}", materialized.Count, endpoint);
            return materialized;
        }
        catch (Exception ex)
        {
            // Log and re-throw the exception instead of swallowing it
            HttpErrorHandler.LogAndThrowException(ex, _logger, endpoint, "GET");
            throw; // Unreachable, but compiler requires it
        }
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
        HttpClient httpClient = CreateHttpClient();

        try
        {
            string jsonContent = JsonSerializer.Serialize(request, _jsonOptions);

            // Security: Validate request size to prevent DoS attacks
            int contentSize = Encoding.UTF8.GetByteCount(jsonContent);
            if (contentSize > _resilienceOptions.MaxRequestSizeBytes)
            {
                throw new ArgumentException(
                    $"Request body size ({contentSize} bytes) exceeds maximum allowed size ({_resilienceOptions.MaxRequestSizeBytes} bytes).",
                    nameof(request));
            }

            using StringContent content = new(jsonContent, Encoding.UTF8, "application/json");
            using HttpRequestMessage httpRequest = new(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            configureRequest?.Invoke(httpRequest);

            using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            // Throw exception if response is not successful
            await HttpErrorHandler.HandleResponseAndThrowAsync(response, _logger, endpoint, "POST").ConfigureAwait(false);

            // Security: Validate response size before reading to prevent DoS attacks
            long? contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > _resilienceOptions.MaxResponseSizeBytes)
            {
                throw new HttpRequestException(
                    $"Response body size ({contentLength} bytes) exceeds maximum allowed size ({_resilienceOptions.MaxResponseSizeBytes} bytes).");
            }

            // Deserialize response
            TResponse? result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Successfully posted to {Endpoint}", endpoint);
            return result;
        }
        catch (Exception ex)
        {
            // Log and re-throw the exception instead of swallowing it
            HttpErrorHandler.LogAndThrowException(ex, _logger, endpoint, "POST");
            throw; // Unreachable, but compiler requires it
        }
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
        HttpClient httpClient = CreateHttpClient();

        try
        {
            string jsonContent = JsonSerializer.Serialize(request, _jsonOptions);

            // Security: Validate request size to prevent DoS attacks
            int contentSize = Encoding.UTF8.GetByteCount(jsonContent);
            if (contentSize > _resilienceOptions.MaxRequestSizeBytes)
            {
                throw new ArgumentException(
                    $"Request body size ({contentSize} bytes) exceeds maximum allowed size ({_resilienceOptions.MaxRequestSizeBytes} bytes).",
                    nameof(request));
            }

            using StringContent content = new(jsonContent, Encoding.UTF8, "application/json");
            using HttpRequestMessage httpRequest = new(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            configureRequest?.Invoke(httpRequest);

            using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            // Throw exception if response is not successful
            await HttpErrorHandler.HandleResponseAndThrowAsync(response, _logger, endpoint, "POST").ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            // Log and re-throw the exception instead of swallowing it
            HttpErrorHandler.LogAndThrowException(ex, _logger, endpoint, "POST");
            throw; // Unreachable, but compiler requires it
        }
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
        HttpClient httpClient = CreateHttpClient();

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Post, endpoint);
            configureRequest?.Invoke(request);

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Throw exception if response is not successful
            await HttpErrorHandler.HandleResponseAndThrowAsync(response, _logger, endpoint, "POST").ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            // Log and re-throw the exception instead of swallowing it
            HttpErrorHandler.LogAndThrowException(ex, _logger, endpoint, "POST");
            throw; // Unreachable, but compiler requires it
        }
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

