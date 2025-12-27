using Npgsql;

namespace WebShop.Util.Models;

/// <summary>
/// Database connection settings model for read and write connections
/// </summary>
public class DbConnectionModel
{
    /// <summary>
    /// Read connection settings (for read-only operations)
    /// </summary>
    public ConnectionModel Read { get; set; } = new();

    /// <summary>
    /// Write connection settings (for write operations)
    /// </summary>
    public ConnectionModel Write { get; set; } = new();

    /// <summary>
    /// Creates a PostgreSQL connection string from connection model
    /// </summary>
    /// <param name="databaseConnectionModel">Connection model with database settings</param>
    /// <param name="applicationNameOverride">Optional application name override from AppSettings. If not provided, uses connection-specific ApplicationName or defaults to "WebShop.Api"</param>
    /// <returns>PostgreSQL connection string</returns>
    public static string CreateConnectionString(ConnectionModel databaseConnectionModel, string? applicationNameOverride = null)
    {
        if (databaseConnectionModel == null)
        {
            throw new ArgumentNullException(nameof(databaseConnectionModel));
        }

        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = databaseConnectionModel.Host,
            Database = databaseConnectionModel.DatabaseName,
            Username = databaseConnectionModel.UserId,
            Password = databaseConnectionModel.Password,

            // Security: SSL/TLS mode (default to Require for production security)
            // IMPORTANCE: Prevents man-in-the-middle attacks and ensures encrypted data transmission.
            // In production, use "VerifyFull" to also validate certificate authority.
            // Without SSL, database credentials and data are transmitted in plain text.
            SslMode = ParseSslMode(databaseConnectionModel.SslMode),

            // Performance: Connection pool settings (with defaults)
            // IMPORTANCE: Connection pooling significantly improves performance by reusing existing connections
            // instead of creating new ones for each request. Reduces connection overhead from ~100-300ms to ~1-5ms.
            // MaxPoolSize: Limits total connections to prevent database overload and resource exhaustion.
            // MinPoolSize: Maintains warm connections for faster response times under low load.
            MaxPoolSize = databaseConnectionModel.MaxPoolSize ?? 100,
            MinPoolSize = databaseConnectionModel.MinPoolSize ?? 5,

            // IMPORTANCE: Closes idle connections after 5 minutes to free resources and prevent connection leaks.
            // Helps maintain optimal connection pool size and prevents database connection exhaustion.
            ConnectionIdleLifetime = databaseConnectionModel.ConnectionIdleLifetime ?? 300,

            // Timeouts (with defaults)
            // IMPORTANCE: CommandTimeout prevents long-running queries from blocking connections indefinitely.
            // Without timeout, a slow query can hold a connection for hours, causing pool exhaustion and application hangs.
            // 30 seconds is a reasonable default - adjust based on your longest expected query duration.
            CommandTimeout = databaseConnectionModel.CommandTimeout ?? 30,
            // IMPORTANCE: ConnectionTimeout prevents the application from hanging if the database is unreachable.
            // Fails fast (15 seconds) instead of waiting indefinitely, allowing proper error handling and retry logic.
            Timeout = databaseConnectionModel.Timeout ?? 15,

            // Performance: Connection lifetime (default: unlimited)
            // IMPORTANCE: 0 = unlimited means connections are recycled by the pool when needed.
            // Prevents connection staleness issues while allowing the pool to manage connection lifecycle efficiently.
            // Non-zero values can help with load balancer connection distribution but are rarely needed.
            ConnectionLifetime = databaseConnectionModel.ConnectionLifetime ?? 0,

            // Application name for monitoring (default: from AppSettings or "WebShop.Api")
            // IMPORTANCE: Appears in PostgreSQL pg_stat_activity view, enabling identification of connections by application.
            // Critical for monitoring, debugging connection issues, and identifying which application is consuming database resources.
            // Helps distinguish between read/write connections and different application instances in distributed systems.
            // Priority: 1. Connection-specific ApplicationName, 2. Global AppSettings.ApplicationName, 3. Default "WebShop.Api"
            ApplicationName = databaseConnectionModel.ApplicationName ?? applicationNameOverride ?? "WebShop.Api",

            // Performance: Prepared statements (with defaults)
            // IMPORTANCE: Prepared statements improve performance by allowing PostgreSQL to cache query plans.
            // Reduces query parsing overhead and protects against SQL injection attacks.
            // MaxAutoPrepare: Limits memory usage while still benefiting from plan caching for frequently used queries.
            // AutoPrepareMinUsages: Ensures only frequently-used queries are prepared, avoiding overhead for one-off queries.
            MaxAutoPrepare = databaseConnectionModel.MaxAutoPrepare ?? 10,
            AutoPrepareMinUsages = databaseConnectionModel.AutoPrepareMinUsages ?? 2
        };

        if (!string.IsNullOrEmpty(databaseConnectionModel.Port) && int.TryParse(databaseConnectionModel.Port, out int port))
        {
            builder.Port = port;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Parses SSL mode string to SslMode enum. Defaults to Require for security.
    /// </summary>
    /// <param name="sslMode">SSL mode string from configuration</param>
    /// <returns>Parsed SslMode enum value</returns>
    private static SslMode ParseSslMode(string? sslMode)
    {
        if (string.IsNullOrWhiteSpace(sslMode))
        {
            return SslMode.Prefer; // Default to secure
        }

        return sslMode.ToUpperInvariant() switch
        {
            "DISABLE" => SslMode.Disable,
            "ALLOW" => SslMode.Allow,
            "PREFER" => SslMode.Prefer,
            "REQUIRE" => SslMode.Require,
            "VERIFYCA" => SslMode.VerifyCA,
            "VERIFYFULL" => SslMode.VerifyFull,
            _ => SslMode.Require // Default to secure if invalid value
        };
    }
}

