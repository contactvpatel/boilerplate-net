using System.Data;

namespace WebShop.Infrastructure.Interfaces;

/// <summary>
/// Factory for creating database connections with read/write separation.
/// </summary>
public interface IDapperConnectionFactory
{
    /// <summary>
    /// Creates a database connection for read operations.
    /// Uses the read connection string from configuration.
    /// </summary>
    /// <returns>A new IDbConnection for read operations.</returns>
    IDbConnection CreateReadConnection();

    /// <summary>
    /// Creates a database connection for write operations.
    /// Uses the write connection string from configuration.
    /// </summary>
    /// <returns>A new IDbConnection for write operations.</returns>
    IDbConnection CreateWriteConnection();
}
