namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for ASP.NET Core Rate Limiting.
/// Follows Microsoft guidelines for rate limiting implementation.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Whether rate limiting is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Global rate limiting policy configuration.
    /// </summary>
    public RateLimitPolicy GlobalPolicy { get; set; } = new();

    /// <summary>
    /// Strict rate limiting policy for sensitive endpoints (e.g., authentication, write operations).
    /// </summary>
    public RateLimitPolicy StrictPolicy { get; set; } = new()
    {
        PermitLimit = 10,
        WindowMinutes = 1,
        QueueLimit = 0
    };

    /// <summary>
    /// Permissive rate limiting policy for read-only endpoints.
    /// </summary>
    public RateLimitPolicy PermissivePolicy { get; set; } = new()
    {
        PermitLimit = 200,
        WindowMinutes = 1,
        QueueLimit = 10
    };
}

