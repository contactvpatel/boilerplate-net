using System.Data;
using System.Runtime.ExceptionServices;
using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using WebShop.Api.Extensions.Utilities;
using WebShop.Util;
using WebShop.Util.Models;

namespace WebShop.Api.HostedServices;

/// <summary>
/// Hosted service that runs database migrations using DbUp.
/// Executes after <see cref="DatabaseConnectionValidationHostedService"/> to ensure connections are valid before migrating.
/// Uses IHostApplicationLifetime.StopApplication() on failure for graceful shutdown (industry standard).
/// </summary>
public class DatabaseMigrationHostedService(
    IOptionsMonitor<AppSettingModel> appSettingModel,
    IConfiguration configuration,
    IHostApplicationLifetime hostLifetime,
    ILogger<DatabaseMigrationHostedService> logger) : IHostedService
{
    private const string DbUpFolderName = "DbUpMigration";
    private const string MigrationsSubfolder = "Migrations";
    private const string SeedsSubfolder = "Seeds";
    private const int LockTimeoutSeconds = 60;
    private const int LockRetryDelayMs = 5000;
    private static readonly TimeSpan MigrationExecutionTimeout = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!appSettingModel.CurrentValue.EnableDatabaseMigration)
        {
            logger.LogDebug("Database migration is disabled; skipping.");
            return;
        }

        logger.LogInformation("Database migration is enabled; starting the migration process.");

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

        if (string.IsNullOrEmpty(dbConnectionString))
        {
            logger.LogWarning("Database write connection string is not configured; skipping migration.");
            return;
        }

        DbUpLoggerExtension dbUpLogger = new(logger);
        string migrationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbUpFolderName, MigrationsSubfolder);

        if (!Directory.Exists(migrationPath) || !Directory.EnumerateFiles(migrationPath, "*.sql").Any())
        {
            logger.LogInformation("No database migration script found; skipping the migration process.");
            return;
        }

        EnsureDatabase.For.PostgresqlDatabase(dbConnectionString, dbUpLogger);

        using NpgsqlConnection dbLockConnection = new(dbConnectionString);
        await dbLockConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        bool lockAcquired = false;
        DateTime startTime = DateTime.UtcNow;
        long lockKey = appSettingModel.CurrentValue.PostgresqlAdvisoryLockKey;

        while ((DateTime.UtcNow - startTime).TotalSeconds < LockTimeoutSeconds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using NpgsqlCommand lockCommand = new(GetAdvisoryLockSql(lockKey, acquire: true), dbLockConnection);
            bool result = lockCommand.ExecuteScalar() as bool? ?? false;

            if (result)
            {
                lockAcquired = true;
                break;
            }

            await Task.Delay(LockRetryDelayMs, cancellationToken).ConfigureAwait(false);
        }

        if (!lockAcquired)
        {
            logger.LogWarning("Could not acquire migration lock within timeout; another instance may be migrating.");
            return;
        }

        try
        {
            UpgradeEngine migrationUpgrader = DeployChanges.To
                .PostgresqlDatabase(dbConnectionString)
                .WithScriptsFromFileSystem(migrationPath)
                .WithTransaction()
                .WithExecutionTimeout(MigrationExecutionTimeout)
                .LogTo(dbUpLogger)
                .Build();

            if (!migrationUpgrader.IsUpgradeRequired())
            {
                logger.LogInformation("No pending migrations detected; skipping the migration process.");
                SeedData(dbConnectionString);
            }
            else
            {
                logger.LogInformation("New database migrations found; initiating the migration process.");
                DatabaseUpgradeResult operation = migrationUpgrader.PerformUpgrade();

                if (operation.Successful)
                {
                    logger.LogInformation("Database migration has been successfully completed.");
                    SeedData(dbConnectionString);
                }
                else
                {
                    logger.LogError(operation.Error, "Database migration has failed");
                    CleanupResources(dbLockConnection);
                    hostLifetime.StopApplication();
                    throw new InvalidOperationException("Database migration failed. Application is stopping.", operation.Error);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception occurred during the database migration process.");
            CleanupResources(dbLockConnection);
            hostLifetime.StopApplication();
            throw;
        }
        finally
        {
            CleanupResources(dbLockConnection);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void SeedData(string dbConnectionString)
    {
        if (string.IsNullOrEmpty(appSettingModel.CurrentValue.Environment))
        {
            return;
        }

        logger.LogInformation("Seeding data for environment: {Environment}", appSettingModel.CurrentValue.Environment);

        DbUpLoggerExtension dbUpLogger = new(logger);
        string seedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbUpFolderName, SeedsSubfolder, appSettingModel.CurrentValue.Environment);

        if (!Directory.Exists(seedPath) || !Directory.EnumerateFiles(seedPath, "*.sql").Any())
        {
            logger.LogInformation("No seed data script found; skipping the seeding process.");
            return;
        }

        UpgradeEngine seedUpgrader = DeployChanges.To
            .PostgresqlDatabase(dbConnectionString)
            .WithScriptsFromFileSystem(seedPath)
            .WithTransaction()
            .WithExecutionTimeout(MigrationExecutionTimeout)
            .LogTo(dbUpLogger)
            .WithVariablesDisabled()
            .Build();

        if (!seedUpgrader.IsUpgradeRequired())
        {
            logger.LogInformation("No new seed data script found; skipping the seeding process.");
            return;
        }

        DatabaseUpgradeResult seedResult = seedUpgrader.PerformUpgrade();

        if (seedResult.Successful)
        {
            logger.LogInformation("Data seeding for {Environment} completed successfully!", appSettingModel.CurrentValue.Environment);
        }
        else
        {
            logger.LogError(seedResult.Error, "An error occurred during data seeding for {Environment}", appSettingModel.CurrentValue.Environment);
            ExceptionDispatchInfo.Capture(seedResult.Error).Throw();
        }
    }

    private void CleanupResources(NpgsqlConnection dbLockConnection)
    {
        if (dbLockConnection != null && dbLockConnection.State == ConnectionState.Open)
        {
            using NpgsqlCommand unlockCommand = new(
                GetAdvisoryLockSql(appSettingModel.CurrentValue.PostgresqlAdvisoryLockKey, acquire: false),
                dbLockConnection);
            unlockCommand.ExecuteNonQuery();
            dbLockConnection.Close();
        }
    }

    private static string GetAdvisoryLockSql(long lockKey, bool acquire)
    {
        return acquire
            ? $"SELECT pg_try_advisory_lock({lockKey});"
            : $"SELECT pg_advisory_unlock({lockKey});";
    }
}
