namespace WebShop.Util.Models;

/// <summary>
/// Individual database connection settings.
/// </summary>
public class ConnectionModel
{
    /// <summary>
    /// Database host
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Database port
    /// </summary>
    public string Port { get; set; } = "5432";

    /// <summary>
    /// Database name
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// SSL mode for the connection. Options: Disable, Allow, Prefer, Require, VerifyCA, VerifyFull
    /// Default: Require (for production security)
    /// </summary>
    public string? SslMode { get; set; }

    /// <summary>
    /// Maximum number of connections in the pool. Default: 100
    /// </summary>
    public int? MaxPoolSize { get; set; }

    /// <summary>
    /// Minimum number of connections to maintain in the pool. Default: 5
    /// </summary>
    public int? MinPoolSize { get; set; }

    /// <summary>
    /// Time in seconds to keep idle connections alive before closing. Default: 300 (5 minutes)
    /// </summary>
    public int? ConnectionIdleLifetime { get; set; }

    /// <summary>
    /// Command timeout in seconds. Default: 30
    /// </summary>
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// Connection timeout in seconds. Default: 15
    /// </summary>
    public int? Timeout { get; set; }

    /// <summary>
    /// Connection lifetime in seconds. 0 = unlimited (connections recycled by pool). Default: 0
    /// </summary>
    public int? ConnectionLifetime { get; set; }

    /// <summary>
    /// Application name for monitoring and connection identification. Default: "WebShop.Api"
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Maximum number of prepared statements to cache. Default: 10
    /// </summary>
    public int? MaxAutoPrepare { get; set; }

    /// <summary>
    /// Minimum number of times a statement must be used before auto-preparing. Default: 2
    /// </summary>
    public int? AutoPrepareMinUsages { get; set; }
}
