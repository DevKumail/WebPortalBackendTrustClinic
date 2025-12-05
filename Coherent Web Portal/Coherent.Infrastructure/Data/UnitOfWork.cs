using Coherent.Core.Interfaces;
using System.Data;

namespace Coherent.Infrastructure.Data;

/// <summary>
/// Unit of Work implementation for managing database transactions
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(IDbConnection connection)
    {
        _connection = connection;
        _connection.Open();
    }

    public IDbConnection Connection => _connection ?? throw new ObjectDisposedException(nameof(UnitOfWork));
    
    public IDbTransaction Transaction => _transaction ?? throw new InvalidOperationException("Transaction not started");

    public void BeginTransaction()
    {
        _transaction = _connection?.BeginTransaction() ?? throw new ObjectDisposedException(nameof(UnitOfWork));
    }

    public void Commit()
    {
        try
        {
            _transaction?.Commit();
        }
        catch
        {
            _transaction?.Rollback();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }
}
