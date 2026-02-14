using System.Data;
using Npgsql;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Shared helper for validating database connections asynchronously.
/// Used by DapperHealthCheck and DatabaseConnectionValidationHostedService to avoid sync-over-async.
/// </summary>
public static class DatabaseConnectionValidator
{
    private const string ValidationSql = "SELECT 1";

    /// <summary>
    /// Validates a database connection by opening it (if closed) and executing a simple query.
    /// Uses async Npgsql APIs when the connection is NpgsqlConnection.
    /// </summary>
    /// <param name="connection">The connection to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when connection is not NpgsqlConnection (async path not supported).</exception>
    public static async Task ValidateAsync(IDbConnection connection, CancellationToken cancellationToken = default)
    {
        if (connection is NpgsqlConnection npgsqlConnection)
        {
            if (npgsqlConnection.State != ConnectionState.Open)
            {
                await npgsqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            await using NpgsqlCommand command = new(ValidationSql, npgsqlConnection);
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using IDbCommand command = connection.CreateCommand();
            command.CommandText = ValidationSql;
            await Task.Run(() => command.ExecuteScalar(), cancellationToken).ConfigureAwait(false);
        }
    }
}
