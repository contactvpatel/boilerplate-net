using WebShop.Core.Interfaces.Base;
using WebShop.Infrastructure.Interfaces;

namespace WebShop.Infrastructure;

/// <summary>
/// Unit of work implementation that delegates to IDapperTransactionManager.
/// Enables batch operations to run in a single transaction.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IDapperTransactionManager _transactionManager;
    private bool _disposed;

    public UnitOfWork(IDapperTransactionManager transactionManager)
    {
        _transactionManager = transactionManager;
    }

    /// <inheritdoc />
    public void BeginTransaction()
    {
        _transactionManager.BeginTransaction();
    }

    /// <inheritdoc />
    public void Commit()
    {
        _transactionManager.Commit();
    }

    /// <inheritdoc />
    public void Rollback()
    {
        _transactionManager.Rollback();
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
