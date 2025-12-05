using System.Data;

namespace Coherent.Core.Interfaces;

/// <summary>
/// Generic repository interface for database operations using Dapper
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, IDbTransaction? transaction = null);
    Task<IEnumerable<T>> GetAllAsync(IDbTransaction? transaction = null);
    Task<Guid> AddAsync(T entity, IDbTransaction? transaction = null);
    Task<bool> UpdateAsync(T entity, IDbTransaction? transaction = null);
    Task<bool> DeleteAsync(Guid id, IDbTransaction? transaction = null);
    Task<IEnumerable<T>> QueryAsync(string sql, object? parameters = null, IDbTransaction? transaction = null);
    Task<T?> QueryFirstOrDefaultAsync(string sql, object? parameters = null, IDbTransaction? transaction = null);
}
