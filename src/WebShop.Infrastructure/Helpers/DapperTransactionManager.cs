using System.Data;
using Microsoft.Extensions.Logging;
using WebShop.Infrastructure.Interfaces;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Transaction manager implementation for Dapper operations with scoped lifecycle management.
/// </summary>
public class DapperTransactionManager : IDapperTransactionManager, IDisposable
{
    private readonly IDapperConnectionFactory _connectionFactory;
    private readonly ILogger<DapperTransactionManager>? _logger;
    private IDbConnection? _writeConnection;
    private IDbTransaction? _currentTransaction;
    private bool _disposed;

    public DapperTransactionManager(
        IDapperConnectionFactory connectionFactory,
        ILogger<DapperTransactionManager>? logger = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger;
    }

    public IDbTransaction BeginTransaction()
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already active. Nested transactions are not supported.");
        }

        _writeConnection = _connectionFactory.CreateWriteConnection();
        _writeConnection.Open();
        _currentTransaction = _writeConnection.BeginTransaction();

        _logger?.LogDebug("Transaction started");

        return _currentTransaction;
    }

    public IDbTransaction? GetCurrentTransaction()
    {
        return _currentTransaction;
    }

    public void Commit()
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            _currentTransaction.Commit();
            _logger?.LogDebug("Transaction committed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error committing transaction");
            throw;
        }
        finally
        {
            DisposeTransaction();
        }
    }

    public void Rollback()
    {
        if (_currentTransaction == null)
        {
            _logger?.LogWarning("Rollback called but no active transaction exists");
            return;
        }

        try
        {
            _currentTransaction.Rollback();
            _logger?.LogDebug("Transaction rolled back");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rolling back transaction");
            throw;
        }
        finally
        {
            DisposeTransaction();
        }
    }

    private void DisposeTransaction()
    {
        _currentTransaction?.Dispose();
        _currentTransaction = null;

        _writeConnection?.Close();
        _writeConnection?.Dispose();
        _writeConnection = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_currentTransaction != null)
        {
            try
            {
                _currentTransaction.Rollback();
                _logger?.LogWarning("Transaction was rolled back during disposal");
            }
            catch
            {
                // Ignore errors during disposal
            }
        }

        DisposeTransaction();
        _disposed = true;
    }
}
