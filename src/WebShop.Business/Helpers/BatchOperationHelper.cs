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

    /// <summary>
    /// Executes a batch of items within a transaction, applying the given operation to each item.
    /// Eliminates repeated foreach + AddAsync/UpdateAsync/DeleteAsync loops across services.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="unitOfWork">The unit of work for transaction management.</param>
    /// <param name="items">The items to process.</param>
    /// <param name="operation">The async operation to perform on each item (e.g., repository.AddAsync).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ExecuteBatchAsync<T>(
        IUnitOfWork unitOfWork,
        IReadOnlyList<T> items,
        Func<T, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return;
        }

        await ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (T item in items)
            {
                await operation(item, ct).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}
