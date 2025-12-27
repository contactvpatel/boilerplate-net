using System.Data;

namespace WebShop.Infrastructure.Interfaces;

/// <summary>
/// Manages database transactions for Dapper operations.
/// Provides scoped transaction management for request-based workflows.
/// </summary>
public interface IDapperTransactionManager
{
    /// <summary>
    /// Begins a new transaction on the write connection.
    /// </summary>
    /// <returns>A database transaction.</returns>
    IDbTransaction BeginTransaction();

    /// <summary>
    /// Gets the current transaction if one exists.
    /// </summary>
    /// <returns>The current transaction, or null if no transaction is active.</returns>
    IDbTransaction? GetCurrentTransaction();

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    void Commit();

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    void Rollback();

    /// <summary>
    /// Disposes the transaction manager and closes connections.
    /// </summary>
    void Dispose();
}
