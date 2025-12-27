using System.Text.Json.Serialization;

namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for HTTP client resilience policies (retry and circuit breaker).
/// Aligned with Microsoft.Extensions.Http.Resilience terminology.
/// </summary>
public class HttpResilienceOptions
{
    /// <summary>
    /// Maximum request body size in bytes to prevent DoS attacks.
    /// Default: 1 MB (1,048,576 bytes).
    /// Used for: Kestrel MaxRequestBodySize, FormOptions, and middleware validation.
    /// </summary>
    public int MaxRequestSizeBytes { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Maximum total size of request headers in bytes.
    /// Default: 32 KB (32,768 bytes).
    /// Used for: Kestrel MaxRequestHeadersTotalSize.
    /// </summary>
    public int MaxRequestHeadersTotalSize { get; set; } = 32 * 1024; // 32KB

    /// <summary>
    /// Maximum number of request headers allowed.
    /// Default: 100 headers.
    /// Used for: Kestrel MaxRequestHeaderCount.
    /// </summary>
    public int MaxRequestHeaderCount { get; set; } = 100;

    /// <summary>
    /// Maximum number of form values allowed in a request.
    /// Default: 1024 values.
    /// Used for: FormOptions ValueCountLimit.
    /// </summary>
    public int MaxFormValueCount { get; set; } = 1024;

    /// <summary>
    /// Maximum response body size in bytes to prevent DoS attacks.
    /// Default: 10 MB (10,485,760 bytes).
    /// </summary>
    public int MaxResponseSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Maximum number of concurrent connections per server.
    /// Default: 10 (increased from default 2 for better concurrency).
    /// </summary>
    public int MaxConnectionsPerServer { get; set; } = 10;

    /// <summary>
    /// Maximum number of retry attempts for transient failures.
    /// Maps to: HttpRetryStrategyOptions.MaxRetryAttempts
    /// Default: 3 retries.
    /// </summary>
    [JsonPropertyName("RetryCount")]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay in seconds for exponential backoff between retry attempts.
    /// Maps to: HttpRetryStrategyOptions.Delay
    /// Default: 2 seconds (results in 2s, 4s, 8s delays with exponential backoff).
    /// </summary>
    [JsonPropertyName("RetryDelaySeconds")]
    public int RetryBaseDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Minimum number of requests required before circuit breaker can evaluate failures.
    /// Maps to: HttpCircuitBreakerStrategyOptions.MinimumThroughput
    /// Default: 5 requests.
    /// Note: Circuit breaker needs this many requests in the sampling window before it can open.
    /// </summary>
    [JsonPropertyName("CircuitBreakerFailureThreshold")]
    public int CircuitBreakerMinimumThroughput { get; set; } = 5;

    /// <summary>
    /// Duration in seconds that circuit breaker stays open before attempting to close.
    /// Maps to: HttpCircuitBreakerStrategyOptions.BreakDuration
    /// Default: 30 seconds.
    /// </summary>
    [JsonPropertyName("CircuitBreakerDurationSeconds")]
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;
}

