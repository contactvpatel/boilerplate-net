using System.Data;
using DbUp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using WebShop.Util;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.DbUp.Core;

public class DatabaseMigrationInitFilter(
    IOptionsMonitor<AppSettingModel> appSettingModel,
    IOptionsMonitor<DatabaseMigrationOptions> databaseMigrationOptions,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<DatabaseMigrationInitFilter> logger) : IStartupFilter
{
    private static readonly string BaseNamespace = typeof(DatabaseMigrationInitFilter).Namespace!
        .Replace(".Core", ""); // WebShop.Infrastructure.DbUp

    private static readonly string MigrationsNamespace = $"{BaseNamespace}.Migrations";
    private static readonly string SeedsNamespacePrefix = $"{BaseNamespace}.Seeds.";

    private readonly IOptionsMonitor<DatabaseMigrationOptions> _options = databaseMigrationOptions ?? throw new ArgumentNullException(nameof(databaseMigrationOptions));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly ILogger<DatabaseMigrationInitFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        if (!_options.CurrentValue.Enabled)
        {
            _logger.LogInformation("Database migration is disabled; skipping migration and seed.");
            return next;
        }

        return app =>
        {
            RunMigrationsAndSeeds(app);
            next(app);
        };
    }

    private void RunMigrationsAndSeeds(IApplicationBuilder app)
    {
        var writeSection = _configuration.GetSection("DbConnectionSettings:Write");
        DbConnectionModel? databaseConnectionSettings = configuration.GetSection(ConfigurationKeys.DatabaseConnectionSettings)
            .Get<DbConnectionModel>() ?? configuration.GetSection(ConfigurationKeys.DbConnectionSettings)
            .Get<DbConnectionModel>();

        if (databaseConnectionSettings == null)
        {
            logger.LogWarning("Database connection settings not found; skipping migration.");
            return;
        }

        string dbConnectionString = DbConnectionModel.CreateConnectionString(
            databaseConnectionSettings.Write,
            appSettingModel.CurrentValue.ApplicationName);

        if (string.IsNullOrWhiteSpace(dbConnectionString))
        {
            _logger.LogInformation("No DbConnectionSettings:Write configured; skipping migration and seed.");
            return;
        }

        var assembly = typeof(DatabaseMigrationInitFilter).Assembly;
        var dbUpLogger = new DbUpLoggerExtension(_logger);

        // Ensure database exists
        EnsureDatabase.For.PostgresqlDatabase(dbConnectionString, dbUpLogger);

        using var dbLockConnection = new NpgsqlConnection(dbConnectionString);
        dbLockConnection.Open();

        var lockKey = _options.CurrentValue.PostgresqlAdvisoryLockKey;
        if (!TryAcquireAdvisoryLock(dbLockConnection, lockKey, TimeSpan.FromSeconds(60)))
        {
            _logger.LogWarning("Could not acquire PostgreSQL advisory lock; skipping migration and seed.");
            return;
        }

        try
        {
            var upgrader = DeployChanges.To
                .PostgresqlDatabase(dbConnectionString)
                .WithScripts(new PrefixStrippingScriptProvider(assembly, MigrationsNamespace, s => s.StartsWith(MigrationsNamespace)))
                .WithVariablesDisabled()
                .WithTransaction()
                .WithExecutionTimeout(TimeSpan.FromMinutes(10))
                .LogTo(dbUpLogger)
                .Build();

            if (!upgrader.IsUpgradeRequired())
            {
                _logger.LogInformation("No pending migrations detected; skipping migration.");
                RunSeeds(dbConnectionString, dbUpLogger);
                return;
            }

            _logger.LogInformation("Pending migrations found; starting migration.");
            var result = upgrader.PerformUpgrade();
            if (result.Successful)
            {
                _logger.LogInformation("Database migrations completed successfully.");
                RunSeeds(dbConnectionString, dbUpLogger);
            }
            else
            {
                _logger.LogError(result.Error, "Database migration failed.");
                ReleaseAdvisoryLock(dbLockConnection, lockKey);
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during database migration.");
            ReleaseAdvisoryLock(dbLockConnection, lockKey);
            Environment.Exit(1);
        }
        finally
        {
            ReleaseAdvisoryLock(dbLockConnection, lockKey);
        }
    }

    private bool TryAcquireAdvisoryLock(NpgsqlConnection connection, long key, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            using var cmd = new NpgsqlCommand($"SELECT pg_try_advisory_lock({key});", connection);
            var acquired = (bool)cmd.ExecuteScalar()!;
            if (acquired)
            {
                _logger.LogInformation("Acquired PostgreSQL advisory lock {Key}.", key);
                return true;
            }

            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        return false;
    }

    private void ReleaseAdvisoryLock(NpgsqlConnection connection, long key)
    {
        if (connection.State != ConnectionState.Open)
        {
            return;
        }

        using var cmd = new NpgsqlCommand($"SELECT pg_advisory_unlock({key});", connection);
        cmd.ExecuteNonQuery();
        _logger.LogInformation("Released PostgreSQL advisory lock {Key}.", key);
    }

    private void RunSeeds(string connectionString, DbUpLoggerExtension dbUpLogger)
    {
        var environmentName = _environment.EnvironmentName;
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            _logger.LogInformation("No environment name set; skipping seed scripts.");
            return;
        }

        var seedsNamespace = SeedsNamespacePrefix + environmentName + ".";
        var assembly = typeof(DatabaseMigrationInitFilter).Assembly;

        _logger.LogInformation("Running seed scripts for environment {Environment}.", environmentName);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScripts(new PrefixStrippingScriptProvider(assembly, seedsNamespace, s => s.StartsWith(seedsNamespace)))
            .WithVariablesDisabled()
            .WithTransaction()
            .WithExecutionTimeout(TimeSpan.FromMinutes(10))
            .LogTo(dbUpLogger)
            .Build();

        if (!upgrader.IsUpgradeRequired())
        {
            _logger.LogInformation("No new seed data scripts found; skipping seeding for {Environment}.", environmentName);
            return;
        }

        var result = upgrader.PerformUpgrade();
        if (result.Successful)
        {
            _logger.LogInformation("Seed scripts for {Environment} completed successfully.", environmentName);
        }
        else
        {
            _logger.LogError(result.Error, "Seed scripts for {Environment} failed.", environmentName);
            throw result.Error;
        }
    }
}

