using System.Data;

namespace Coherent.Core.Interfaces;

/// <summary>
/// Unit of Work pattern for managing database transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }
    void BeginTransaction();
    void Commit();
    void Rollback();
}
