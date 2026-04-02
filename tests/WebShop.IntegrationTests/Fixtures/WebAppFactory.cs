using DbUp;
using DbUp.Engine.Output;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WebShop.Api;
using WebShop.Util.Models;

namespace WebShop.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration tests with local PostgreSQL.
/// Uses appsettings.Testing.json for DB connection (webshop_test database). No Docker required.
/// Creates webshop_test database if missing. Override via env: INTEGRATION_TEST_DB_HOST, PORT, DATABASE, USER, PASSWORD.
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>
{
    private static bool _databaseEnsured;

    /// <summary>
    /// Ensures the test database exists. Call before CreateClient (e.g. from test constructor).
    /// </summary>
    public static void EnsureTestDatabaseExists()
    {
        if (_databaseEnsured)
        {
            return;
        }

        string connectionString = BuildConnectionStringFromConfig();
        NullUpgradeLog logger = new();
        EnsureDatabase.For.PostgresqlDatabase(connectionString, logger);
        _databaseEnsured = true;
    }

    private static string BuildConnectionStringFromConfig()
    {
        string? host = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_HOST");
        string? port = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_PORT");
        string? database = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_NAME");
        string? user = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_USER");
        string? password = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_PASSWORD");

        if (host != null || database != null || user != null || password != null)
        {
            return new NpgsqlConnectionStringBuilder
            {
                Host = host ?? "localhost",
                Port = int.TryParse(port ?? "5432", out int p) ? p : 5432,
                Database = database ?? "webshop_test",
                Username = user ?? "postgres",
                Password = password ?? "postgres",
                SslMode = SslMode.Disable
            }.ConnectionString;
        }

        string basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? AppContext.BaseDirectory;
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(basePath!)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Testing.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        ConnectionModel? write = config.GetSection("DbConnectionSettings:Write").Get<ConnectionModel>() ?? throw new InvalidOperationException("DbConnectionSettings:Write not configured. Add appsettings.Testing.json or set INTEGRATION_TEST_DB_* env vars.");
        return DbConnectionModel.CreateConnectionString(write, "webshop-integration-test");
    }

    private sealed class NullUpgradeLog : IUpgradeLog
    {
        public void LogTrace(string format, params object[] args) { }
        public void LogDebug(string format, params object[] args) { }
        public void LogInformation(string format, params object[] args) { }
        public void LogWarning(string format, params object[] args) { }
        public void LogError(string format, params object[] args) { }
        public void LogError(Exception ex, string format, params object[] args) { }
    }
    /// <summary>
    /// Resets the database to a fresh state by truncating all tables.
    /// Call at the start of each test for isolation (Phase 2.3 checklist).
    /// Retries on deadlock (40P01) since tests may run in parallel.
    /// </summary>
    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        string connectionString = GetConnectionString();
        const string sql = """
            TRUNCATE TABLE
                webshop.order_positions,
                webshop."order",
                webshop.stock,
                webshop.articles,
                webshop.address,
                webshop.customer,
                webshop.products,
                webshop.labels,
                webshop.colors,
                webshop.sizes
            RESTART IDENTITY CASCADE
            """;

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await using NpgsqlConnection connection = new(connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using NpgsqlCommand cmd = new(sql, connection);
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == "40P01" && attempt < 3)
            {
                await Task.Delay(100 * attempt, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private string GetConnectionString()
    {
        // Allow env override for CI
        string? host = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_HOST");
        string? port = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_PORT");
        string? database = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_NAME");
        string? user = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_USER");
        string? password = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_PASSWORD");

        if (host != null || database != null || user != null || password != null)
        {
            return new NpgsqlConnectionStringBuilder
            {
                Host = host ?? "localhost",
                Port = int.TryParse(port ?? "5432", out int p) ? p : 5432,
                Database = database ?? "webshop_test",
                Username = user ?? "postgres",
                Password = password ?? "postgres",
                SslMode = SslMode.Disable
            }.ConnectionString;
        }

        // Use config from running server (appsettings.Testing.json)
        using IServiceScope scope = Server.Services.CreateScope();
        IConfiguration config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        ConnectionModel? write = config.GetSection("DbConnectionSettings:Write").Get<ConnectionModel>() ?? throw new InvalidOperationException("DbConnectionSettings:Write not configured. Add appsettings.Testing.json with test database settings.");
        string appName = config.GetValue<string>("AppSettings:ApplicationName") ?? "webshop-integration-test";
        return DbConnectionModel.CreateConnectionString(write, appName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        Dictionary<string, string?> overrides = new Dictionary<string, string?>
        {
            ["AppSettings:Environment"] = "Testing",
            ["AppSettings:EnableAsmAuthorization"] = "false",
            ["CacheOptions:Enabled"] = "false",
            ["OpenTelemetryOptions:EnableTracing"] = "false",
            ["OpenTelemetryOptions:EnableMetrics"] = "false",
            ["OpenTelemetryOptions:EnableLogging"] = "false",
        };

        // Allow CI to override DB connection via env vars
        string? host = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_HOST");
        string? port = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_PORT");
        string? database = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_NAME");
        string? user = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_USER");
        string? password = Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_PASSWORD");
        if (host != null)
        {
            overrides["DbConnectionSettings:Write:Host"] = overrides["DbConnectionSettings:Read:Host"] = host;
        }

        if (port != null)
        {
            overrides["DbConnectionSettings:Write:Port"] = overrides["DbConnectionSettings:Read:Port"] = port;
        }

        if (database != null)
        {
            overrides["DbConnectionSettings:Write:DatabaseName"] = overrides["DbConnectionSettings:Read:DatabaseName"] = database;
        }

        if (user != null)
        {
            overrides["DbConnectionSettings:Write:UserId"] = overrides["DbConnectionSettings:Read:UserId"] = user;
        }

        if (password != null)
        {
            overrides["DbConnectionSettings:Write:Password"] = overrides["DbConnectionSettings:Read:Password"] = password;
        }

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(overrides));
    }
}
