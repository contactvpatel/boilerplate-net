using System.Data;
using System.Runtime.ExceptionServices;
using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Options;
using Npgsql;
using WebShop.Api.Extensions.Utilities;
using WebShop.Util.Models;

namespace WebShop.Api.Filters;

/// <summary>
/// Startup filter that runs database migrations using DbUp
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DatabaseMigrationInitFilter"/> class.
/// </remarks>
/// <param name="appSettingModel">Application settings.</param>
/// <param name="configuration">Configuration.</param>
/// <param name="logger">Logger instance.</param>
public class DatabaseMigrationInitFilter(
    IOptionsMonitor<AppSettingModel> appSettingModel,
    IConfiguration configuration,
    ILogger<DatabaseMigrationInitFilter> logger) : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        if (appSettingModel.CurrentValue.EnableDatabaseMigration)
        {
            logger.LogInformation("Database migration is enabled; starting the migration process.");

            DbConnectionModel databaseConnectionSettings = new();
            configuration.GetSection("DbConnectionSettings").Bind(databaseConnectionSettings);

            // Use global ApplicationName from AppSettings for database migrations
            string dbConnectionString = DbConnectionModel.CreateConnectionString(
                databaseConnectionSettings.Write,
                appSettingModel.CurrentValue.ApplicationName);

            if (string.IsNullOrEmpty(dbConnectionString))
            {
                logger.LogWarning("Database write connection string is not configured; skipping migration.");
                return next;
            }

            DbUpLoggerExtension dbUpLogger = new(logger);

            string migrationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DbUpMigration", "Migrations");

            if (Directory.Exists(migrationPath) && Directory.EnumerateFiles(migrationPath, "*.sql").Any())
            {
                // Ensure Database exists, if not then create it
                EnsureDatabase.For.PostgresqlDatabase(dbConnectionString, dbUpLogger);

                using NpgsqlConnection dbLockConnection = new(dbConnectionString);
                dbLockConnection.Open();

                // Attempt to acquire the advisory lock with a timeout (Ensures only one process executes the DbUp migration if multiple processes are running simultaneously)
                bool lockAcquired = false;
                DateTime startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalSeconds < 60) // Set a 60 seconds timeout for waiting for the lock
                {
                    using NpgsqlCommand lockCommand = new(string.Concat("SELECT pg_try_advisory_lock(", appSettingModel.CurrentValue.PostgresqlAdvisoryLockKey, ");"), dbLockConnection);
                    bool result = lockCommand.ExecuteScalar() as bool? ?? false;

                    if (result)
                    {
                        lockAcquired = true;
                        break;
                    }

                    // Wait for 5 second before retrying
                    Thread.Sleep(5000);
                }

                if (lockAcquired)
                {
                    try
                    {
                        // PostgreSQL migration (current implementation)
                        UpgradeEngine migrationUpgrader = DeployChanges.To
                                                .PostgresqlDatabase(dbConnectionString)
                                                .WithScriptsFromFileSystem(migrationPath)
                                                .WithTransaction()
                                                .WithExecutionTimeout(TimeSpan.FromMinutes(10))
                                                .LogTo(dbUpLogger)
                                                .Build();

                        /* SQL Server migration (uncomment to use):
                        UpgradeEngine migrationUpgrader = DeployChanges.To
                                                .SqlDatabase(dbConnectionString)
                                                .WithScriptsFromFileSystem(migrationPath)
                                                .WithTransaction()
                                                .WithExecutionTimeout(TimeSpan.FromMinutes(10))
                                                .LogTo(dbUpLogger)
                                                .Build();
                        */

                        if (!migrationUpgrader.IsUpgradeRequired())
                        {
                            logger.LogInformation("No pending migrations detected; skipping the migration process.");

                            // Run Data Seed Scripts
                            SeedData(dbConnectionString);
                        }
                        else
                        {
                            logger.LogInformation("New database migrations found; initiating the migration process.");

                            // Run Migration Scripts
                            DatabaseUpgradeResult operation = migrationUpgrader.PerformUpgrade();
                            if (operation.Successful)
                            {
                                logger.LogInformation("Database migration has been successfully completed.");

                                // Run Data Seed Scripts Only if Migration Succeeds
                                SeedData(dbConnectionString);
                            }
                            else
                            {
                                logger.LogError(operation.Error, "Database migration has failed");
                                CleanupResources(dbLockConnection);
                                // Immediately terminate the API if database migration fails to ensure previous stable API keeps running in kubernetes with a valid state
                                Environment.Exit(1);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An exception occurred during the database migration process.");
                        CleanupResources(dbLockConnection);
                        // Immediately terminate the API if database migration fails to ensure previous stable API keeps running in kubernetes with a valid state
                        Environment.Exit(1);
                    }
                    finally
                    {
                        CleanupResources(dbLockConnection);
                    }
                }
            }
            else
            {
                logger.LogInformation("No database migration script found; skipping the migration process.");
            }
        }
        return next;
    }

    private void SeedData(string dbConnectionString)
    {
        if (!string.IsNullOrEmpty(appSettingModel.CurrentValue.Environment))
        {
            logger.LogInformation("Seeding data for environment: {Environment}", appSettingModel.CurrentValue.Environment);

            DbUpLoggerExtension dbUpLogger = new(logger);

            string seedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DbUpMigration", "Seeds", appSettingModel.CurrentValue.Environment);

            if (Directory.Exists(seedPath) && Directory.EnumerateFiles(seedPath, "*.sql").Any())
            {
                UpgradeEngine seedUpgrader = DeployChanges.To
                                    .PostgresqlDatabase(dbConnectionString)
                                    .WithScriptsFromFileSystem(seedPath)
                                    .WithTransaction()
                                    .WithExecutionTimeout(TimeSpan.FromMinutes(10))
                                    .LogTo(dbUpLogger)
                                    .WithVariablesDisabled()  // Disable variable substitution for seed scripts to avoid conflicts with PostgreSQL dollar-quoting
                                    .Build();

                if (!seedUpgrader.IsUpgradeRequired())
                {
                    logger.LogInformation("No new seed data script found; skipping the seeding process.");
                }
                else
                {
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
            }
            else
            {
                logger.LogInformation("No seed data script found; skipping the seeding process.");
            }
        }
    }

    private void CleanupResources(NpgsqlConnection dbLockConnection)
    {
        if (dbLockConnection != null && dbLockConnection.State == ConnectionState.Open)
        {
            // Release the advisory lock after migrations/seeds are done
            using NpgsqlCommand unlockCommand = new(string.Concat("SELECT pg_advisory_unlock(", appSettingModel.CurrentValue.PostgresqlAdvisoryLockKey, ");"), dbLockConnection);
            unlockCommand.ExecuteNonQuery();
            dbLockConnection.Close();
        }
    }
}

