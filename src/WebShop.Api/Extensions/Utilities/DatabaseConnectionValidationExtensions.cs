using System.Data;
using Npgsql;
using WebShop.Infrastructure.Helpers;
using WebShop.Infrastructure.Interfaces;

namespace WebShop.Api.Extensions.Utilities;

/// <summary>
/// Extension methods for validating database connections.
/// </summary>
public static class DatabaseConnectionValidationExtensions
{
    /// <summary>
    /// Validates both read and write database connections using Dapper.
    /// Implements fail-fast pattern to detect connection issues before serving requests.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <exception cref="InvalidOperationException">Thrown when connection validation fails.</exception>
    public static void ValidateDatabaseConnections(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        IServiceProvider services = scope.ServiceProvider;
        ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("DatabaseConnectionValidation");

        logger.LogDebug("Validating database connections...");

        try
        {
            IDapperConnectionFactory connectionFactory = services.GetRequiredService<IDapperConnectionFactory>();

            ValidateConnection(connectionFactory.CreateReadConnection(), "read", logger);
            ValidateConnection(connectionFactory.CreateWriteConnection(), "write", logger);

            logger.LogDebug("Database connections validated successfully");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database connection validation failed. Application will not start.");
            throw;
        }
    }

    /// <summary>
    /// Validates a single database connection.
    /// </summary>
    /// <param name="connection">Database connection to validate.</param>
    /// <param name="connectionType">Type of connection (read/write) for error messages.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="InvalidOperationException">Thrown when connection validation fails.</exception>
    private static void ValidateConnection(
        IDbConnection connection,
        string connectionType,
        ILogger logger)
    {
        try
        {
            using (connection)
            {
                // Use Task.Run to avoid deadlock during synchronous startup context
                // This prevents thread pool starvation and potential deadlocks
                Task.Run(async () =>
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        if (connection is NpgsqlConnection npgsqlConnection)
                        {
                            await npgsqlConnection.OpenAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            connection.Open();
                        }
                    }

                    // Execute a simple query to verify connectivity
                    using IDbCommand command = connection.CreateCommand();
                    command.CommandText = "SELECT 1";
                    await Task.Run(() => command.ExecuteScalar()).ConfigureAwait(false);
                }).GetAwaiter().GetResult();

                logger.LogDebug("Successfully validated {ConnectionType} database connection", connectionType);
            }
        }
        catch (Exception ex)
        {
            string errorMessage = $"Failed to validate {connectionType} database connection: {ex.Message}";

            logger.LogError(ex, "Exception while validating {ConnectionType} database connection", connectionType);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }
}
