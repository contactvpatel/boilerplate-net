using System.Data;
using Npgsql;
using WebShop.Infrastructure.Interfaces;

namespace WebShop.Api.HostedServices;

/// <summary>
/// Hosted service that validates database connections asynchronously at startup.
/// Implements fail-fast pattern to detect connection issues before the application accepts requests.
/// Runs during IHost.StartAsync, avoiding sync-over-async.
/// </summary>
public class DatabaseConnectionValidationHostedService(
    IDapperConnectionFactory connectionFactory,
    ILogger<DatabaseConnectionValidationHostedService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Validating database connections...");

        try
        {
            await ValidateConnectionAsync(connectionFactory.CreateReadConnection(), "read", cancellationToken)
                .ConfigureAwait(false);
            await ValidateConnectionAsync(connectionFactory.CreateWriteConnection(), "write", cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug("Database connections validated successfully");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database connection validation failed. Application will not start.");
            throw;
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ValidateConnectionAsync(
        IDbConnection connection,
        string connectionType,
        CancellationToken cancellationToken)
    {
        using (connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                if (connection is NpgsqlConnection npgsqlConnection)
                {
                    await npgsqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    connection.Open();
                }
            }

            using IDbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await Task.Run(() => command.ExecuteScalar(), cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Successfully validated {ConnectionType} database connection", connectionType);
        }
    }
}
