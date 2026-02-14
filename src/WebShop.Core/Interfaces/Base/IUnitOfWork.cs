namespace WebShop.Core.Interfaces.Base;

/// <summary>
/// Defines a unit of work for coordinating multiple repository operations in a single transaction.
/// Use for batch operations to ensure atomicity - all changes commit together or roll back on failure.
/// </summary>
/// <remarks>
/// Dapper executes SQL immediately; this interface coordinates transaction scope.
/// Call BeginTransaction before batch operations, then Commit or Rollback.
/// </remarks>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Begins a new database transaction. All subsequent repository operations
    /// within the same scope will participate in this transaction.
    /// </summary>
    void BeginTransaction();

    /// <summary>
    /// Commits the current transaction, persisting all changes.
    /// </summary>
    void Commit();

    /// <summary>
    /// Rolls back the current transaction, discarding all changes.
    /// </summary>
    void Rollback();
}
