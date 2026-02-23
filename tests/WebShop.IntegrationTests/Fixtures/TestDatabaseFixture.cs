using System.Data;
using DbUp;
using DbUp.Engine.Output;
using Microsoft.Extensions.Configuration;
using Npgsql;
using WebShop.Infrastructure.Interfaces;
using WebShop.Util.Models;
using Xunit;

namespace WebShop.IntegrationTests.Fixtures;

/// <summary>
/// Fixture for repository tests using real PostgreSQL (same as integration tests).
/// Uses appsettings.Testing.json or INTEGRATION_TEST_DB_* env vars. No Docker required.
/// </summary>
public sealed class TestDatabaseFixture : IAsyncLifetime
{
    private static bool _databaseEnsured;
    private readonly string _connectionString;
    private readonly TestConnectionFactory _connectionFactory;

    public TestDatabaseFixture()
    {
        _connectionString = BuildConnectionString();
        _connectionFactory = new TestConnectionFactory(_connectionString);
    }

    public IDapperConnectionFactory ConnectionFactory => _connectionFactory;

    /// <summary>
    /// Resets the database to a fresh state. Call at the start of each test for isolation.
    /// Retries on deadlock (40P01) for robustness.
    /// </summary>
    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
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
                await using NpgsqlConnection connection = new(_connectionString);
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

    public async Task InitializeAsync()
    {
        if (_databaseEnsured)
        {
            return;
        }

        EnsureDatabase.For.PostgresqlDatabase(_connectionString, new NullUpgradeLog());
        _databaseEnsured = true;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
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

    private static string BuildConnectionString()
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

        string basePath = Path.GetDirectoryName(typeof(TestDatabaseFixture).Assembly.Location) ?? AppContext.BaseDirectory;
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(basePath!)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Testing.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        ConnectionModel? write = config.GetSection("DbConnectionSettings:Write").Get<ConnectionModel>() ?? throw new InvalidOperationException("DbConnectionSettings:Write not configured. Add appsettings.Testing.json or set INTEGRATION_TEST_DB_* env vars.");

        return DbConnectionModel.CreateConnectionString(write, "webshop-repo-test");
    }

    private sealed class TestConnectionFactory(string connectionString) : IDapperConnectionFactory
    {
        private readonly string _connectionString = connectionString;

        public IDbConnection CreateReadConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public IDbConnection CreateWriteConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
