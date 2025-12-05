using Coherent.Core.Interfaces;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

/// <summary>
/// Base repository implementation using Dapper with parameterized queries for SQL injection prevention
/// </summary>
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly IDbConnection _connection;
    protected abstract string TableName { get; }

    protected BaseRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, IDbTransaction? transaction = null)
    {
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }, transaction);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(IDbTransaction? transaction = null)
    {
        var sql = $"SELECT * FROM {TableName}";
        return await _connection.QueryAsync<T>(sql, transaction: transaction);
    }

    public virtual async Task<Guid> AddAsync(T entity, IDbTransaction? transaction = null)
    {
        var properties = GetProperties(entity);
        var columns = string.Join(", ", properties.Select(p => p.Key));
        var values = string.Join(", ", properties.Select(p => $"@{p.Key}"));
        
        var sql = $"INSERT INTO {TableName} ({columns}) VALUES ({values}); SELECT CAST(SCOPE_IDENTITY() AS UNIQUEIDENTIFIER);";
        return await _connection.ExecuteScalarAsync<Guid>(sql, entity, transaction);
    }

    public virtual async Task<bool> UpdateAsync(T entity, IDbTransaction? transaction = null)
    {
        var properties = GetProperties(entity);
        var setClause = string.Join(", ", properties.Where(p => p.Key != "Id").Select(p => $"{p.Key} = @{p.Key}"));
        
        var sql = $"UPDATE {TableName} SET {setClause} WHERE Id = @Id";
        var result = await _connection.ExecuteAsync(sql, entity, transaction);
        return result > 0;
    }

    public virtual async Task<bool> DeleteAsync(Guid id, IDbTransaction? transaction = null)
    {
        var sql = $"DELETE FROM {TableName} WHERE Id = @Id";
        var result = await _connection.ExecuteAsync(sql, new { Id = id }, transaction);
        return result > 0;
    }

    public virtual async Task<IEnumerable<T>> QueryAsync(string sql, object? parameters = null, IDbTransaction? transaction = null)
    {
        return await _connection.QueryAsync<T>(sql, parameters, transaction);
    }

    public virtual async Task<T?> QueryFirstOrDefaultAsync(string sql, object? parameters = null, IDbTransaction? transaction = null)
    {
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, parameters, transaction);
    }

    private Dictionary<string, object?> GetProperties(T entity)
    {
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, p => p.GetValue(entity));
        
        return properties;
    }
}
