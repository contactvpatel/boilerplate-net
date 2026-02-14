using WebShop.Core.Interfaces.Base;

namespace WebShop.Business.Helpers;

/// <summary>
/// Helper for executing batch operations within a transaction scope.
/// Ensures atomicity: all operations commit together or roll back on failure.
/// </summary>
public static class BatchOperationHelper
{
    /// <summary>
    /// Executes an async batch operation within a transaction.
    /// Commits on success, rolls back on exception.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="operation">The batch operation to execute (e.g., multiple repository calls).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ExecuteInTransactionAsync(
        IUnitOfWork unitOfWork,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            unitOfWork.BeginTransaction();
            await operation(cancellationToken).ConfigureAwait(false);
            unitOfWork.Commit();
        }
        catch (Exception)
        {
            unitOfWork.Rollback();
            throw;
        }
    }
}
