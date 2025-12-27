using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Extension methods for HttpClient to simplify common operations.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Sets the Authorization header with a Bearer token.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="token">The bearer token.</param>
    /// <returns>The HTTP client for method chaining.</returns>
    public static HttpClient SetBearerToken(this HttpClient httpClient, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return httpClient;
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return httpClient;
    }

    /// <summary>
    /// Adds a custom header to the HTTP client with security validation.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The HTTP client for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when header name or value contains invalid characters.</exception>
    public static HttpClient AddHeader(this HttpClient httpClient, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
        {
            return httpClient;
        }

        // Security: Validate header name and value to prevent CRLF injection
        if (name.Contains('\r') || name.Contains('\n') || value.Contains('\r') || value.Contains('\n'))
        {
            throw new ArgumentException("Header name or value contains invalid characters (CRLF injection attempt detected).", nameof(name));
        }

        // Security: Validate header name format (RFC 7230: header names must be valid tokens)
        // Valid tokens: alphanumeric and specific special characters, no spaces or control characters
        if (name.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
        {
            throw new ArgumentException($"Invalid header name format: {name}. Header names cannot contain control characters or whitespace.", nameof(name));
        }

        httpClient.DefaultRequestHeaders.Add(name, value);
        return httpClient;
    }

    /// <summary>
    /// Adds multiple headers to the HTTP client.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="headers">Dictionary of header names and values.</param>
    /// <returns>The HTTP client for method chaining.</returns>
    public static HttpClient AddHeaders(this HttpClient httpClient, Dictionary<string, string> headers)
    {
        if (headers == null || headers.Count == 0)
        {
            return httpClient;
        }

        foreach (KeyValuePair<string, string> header in headers)
        {
            if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        return httpClient;
    }

    /// <summary>
    /// Creates a JSON StringContent from an object.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>StringContent with JSON representation of the object.</returns>
    public static StringContent AsJsonContent<T>(this T obj, JsonSerializerOptions? options = null)
    {
        // Performance: Use JsonContext.Default.Options if no options provided (source generator)
        options ??= JsonContext.Default.Options;
        string json = JsonSerializer.Serialize(obj, options);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Ensures the URL ends with a slash.
    /// </summary>
    /// <param name="url">The URL to normalize.</param>
    /// <returns>The normalized URL.</returns>
    public static string EnsureTrailingSlash(this string url)
    {
        return string.IsNullOrWhiteSpace(url) ? url : url.TrimEnd('/') + "/";
    }

    /// <summary>
    /// Ensures the path doesn't start with a slash.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    public static string RemoveLeadingSlash(this string path)
    {
        return string.IsNullOrWhiteSpace(path) ? path : path.TrimStart('/');
    }

    /// <summary>
    /// Combines a base URL with an endpoint path, handling slashes correctly.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="endpoint">The endpoint path.</param>
    /// <returns>The combined URL.</returns>
    public static string CombineUrl(this string baseUrl, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return endpoint ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return baseUrl;
        }

        string normalizedBase = baseUrl.TrimEnd('/');
        string normalizedEndpoint = endpoint.TrimStart('/');
        return $"{normalizedBase}/{normalizedEndpoint}";
    }

    /// <summary>
    /// Sets the Authorization header with a Bearer token on an HttpRequestMessage.
    /// Thread-safe alternative to modifying DefaultRequestHeaders.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="token">The bearer token.</param>
    /// <returns>The HTTP request message for method chaining.</returns>
    public static HttpRequestMessage SetBearerToken(this HttpRequestMessage request, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return request;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    /// <summary>
    /// Adds a custom header to an HttpRequestMessage with security validation.
    /// Thread-safe alternative to modifying DefaultRequestHeaders.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The HTTP request message for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when header name or value contains invalid characters.</exception>
    public static HttpRequestMessage AddHeader(this HttpRequestMessage request, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
        {
            return request;
        }

        // Security: Validate header name and value to prevent CRLF injection
        if (name.Contains('\r') || name.Contains('\n') || value.Contains('\r') || value.Contains('\n'))
        {
            throw new ArgumentException("Header name or value contains invalid characters (CRLF injection attempt detected).", nameof(name));
        }

        // Security: Validate header name format (RFC 7230: header names must be valid tokens)
        if (name.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)))
        {
            throw new ArgumentException($"Invalid header name format: {name}. Header names cannot contain control characters or whitespace.", nameof(name));
        }

        request.Headers.Add(name, value);
        return request;
    }
}

