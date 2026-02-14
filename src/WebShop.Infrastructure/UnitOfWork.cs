using WebShop.Core.Interfaces.Base;
using WebShop.Infrastructure.Interfaces;

namespace WebShop.Infrastructure;

/// <summary>
/// Unit of work implementation that delegates to IDapperTransactionManager.
/// Enables batch operations to run in a single transaction.
/// </summary>
public sealed class UnitOfWork(IDapperTransactionManager transactionManager) : IUnitOfWork
{
    private bool _disposed;

    /// <inheritdoc />
    public void BeginTransaction()
    {
        transactionManager.BeginTransaction();
    }

    /// <inheritdoc />
    public void Commit()
    {
        transactionManager.Commit();
    }

    /// <inheritdoc />
    public void Rollback()
    {
        transactionManager.Rollback();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
