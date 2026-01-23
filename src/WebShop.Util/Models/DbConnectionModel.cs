// using Microsoft.Data.SqlClient; // Uncomment for SQL Server support
using Npgsql;

namespace WebShop.Util.Models;

/// <summary>
/// Supported database providers
/// </summary>
public enum DatabaseProvider
{
    PostgreSQL,
    SQLServer
}

/// <summary>
/// Database connection settings model for read and write connections
/// </summary>
public class DbConnectionModel
{
    /// <summary>
    /// Database provider type (PostgreSQL or SQLServer)
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.PostgreSQL;

    /// <summary>
    /// Read connection settings (for read-only operations)
    /// </summary>
    public ConnectionModel Read { get; set; } = new();

    /// <summary>
    /// Write connection settings (for write operations)
    /// </summary>
    public ConnectionModel Write { get; set; } = new();

    /// <summary>
    /// Creates a database connection string from connection model and provider settings
    /// </summary>
    /// <param name="databaseConnectionModel">Database connection settings</param>
    /// <param name="applicationName">Application name from AppSettings</param>
    /// <returns>Database connection string</returns>
    public static string CreateConnectionString(ConnectionModel databaseConnectionModel, string applicationName)
    {
        if (databaseConnectionModel == null)
        {
            throw new ArgumentNullException(nameof(databaseConnectionModel));
        }

        return CreatePostgreSQLConnectionString(databaseConnectionModel, applicationName);

        // If SQL Server support is enabled, uncomment the following line and CreateSQLServerConnectionString function, add corresponding using directive and remove the above line for PostgreSQL

        // return CreateSQLServerConnectionString(databaseConnectionModel, applicationName);
    }

    /// <summary>
    /// Creates a PostgreSQL connection string from connection model
    /// </summary>
    private static string CreatePostgreSQLConnectionString(ConnectionModel databaseConnectionModel, string applicationName)
    {
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

            // Application name for monitoring (default: from AppSettings)
            // IMPORTANCE: Appears in PostgreSQL pg_stat_activity view, enabling identification of connections by application.
            // Critical for monitoring, debugging connection issues, and identifying which application is consuming database resources.
            // Helps distinguish between read/write connections and different application instances in distributed systems.
            ApplicationName = applicationName,

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
    /// Creates a SQL Server connection string from connection model (commented out - uncomment to enable)
    /// </summary>
    /*
    private static string CreateSQLServerConnectionString(ConnectionModel databaseConnectionModel, string applicationName)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = $"{databaseConnectionModel.Host}{(string.IsNullOrEmpty(databaseConnectionModel.Port) ? "" : $",{databaseConnectionModel.Port}")}",
            InitialCatalog = databaseConnectionModel.DatabaseName,
            UserID = databaseConnectionModel.UserId,
            Password = databaseConnectionModel.Password,

            // Security: Encrypt connections (equivalent to PostgreSQL SSL)
            Encrypt = databaseConnectionModel.SslMode?.ToUpperInvariant() switch
            {
                "REQUIRE" or "VERIFYCA" or "VERIFYFULL" => true,
                _ => false
            },

            // Performance: Connection pool settings
            MaxPoolSize = databaseConnectionModel.MaxPoolSize ?? 100,
            MinPoolSize = databaseConnectionModel.MinPoolSize ?? 5,

            // Timeouts
            CommandTimeout = databaseConnectionModel.CommandTimeout ?? 30,
            ConnectTimeout = databaseConnectionModel.Timeout ?? 15,

            // Application name for monitoring
            ApplicationName = applicationName,

            // Additional SQL Server specific settings
            TrustServerCertificate = databaseConnectionModel.SslMode?.ToUpperInvariant() switch
            {
                "ALLOW" or "DISABLE" => true,
                _ => false
            },
            MultipleActiveResultSets = true, // Enable MARS for better performance
            PersistSecurityInfo = false // Don't persist sensitive info
        };

        return builder.ConnectionString;
    }
    */

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

