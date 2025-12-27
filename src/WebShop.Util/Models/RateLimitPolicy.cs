namespace WebShop.Util.Models;

/// <summary>
/// Configuration for a rate limiting policy.
/// </summary>
public class RateLimitPolicy
{
    /// <summary>
    /// Maximum number of requests allowed in the time window.
    /// Default: 100 requests
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Time window in minutes.
    /// Default: 1 minute
    /// </summary>
    public int WindowMinutes { get; set; } = 1;

    /// <summary>
    /// Maximum number of queued requests when limit is reached.
    /// Default: 0 (reject immediately)
    /// Set to > 0 to queue requests instead of rejecting
    /// </summary>
    public int QueueLimit { get; set; } = 0;

    /// <summary>
    /// Rate limiting algorithm to use.
    /// Options: "FixedWindow", "SlidingWindow", "TokenBucket", "Concurrency"
    /// Default: "FixedWindow"
    /// </summary>
    public string Algorithm { get; set; } = "FixedWindow";

    /// <summary>
    /// For TokenBucket: Number of tokens to add per replenishment period.
    /// Default: 10
    /// </summary>
    public int TokensPerPeriod { get; set; } = 10;

    /// <summary>
    /// For TokenBucket: Replenishment period in seconds.
    /// Default: 10 seconds
    /// </summary>
    public int ReplenishmentPeriodSeconds { get; set; } = 10;

    /// <summary>
    /// For TokenBucket: Maximum number of tokens in the bucket.
    /// Default: 20
    /// </summary>
    public int TokenLimit { get; set; } = 20;

    /// <summary>
    /// For Concurrency: Maximum number of concurrent requests.
    /// Default: 10
    /// </summary>
    public int PermitLimitConcurrency { get; set; } = 10;

    /// <summary>
    /// For Concurrency: Maximum number of queued requests.
    /// Default: 0
    /// </summary>
    public int QueueLimitConcurrency { get; set; } = 0;
}
