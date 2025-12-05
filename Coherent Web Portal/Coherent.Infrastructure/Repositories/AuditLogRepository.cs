using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

/// <summary>
/// ADHICS Compliance: Audit log repository for compliance logging
/// </summary>
public class AuditLogRepository : BaseRepository<AuditLog>
{
    protected override string TableName => "AuditLogs";

    public AuditLogRepository(IDbConnection connection) : base(connection)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(
        DateTime from, 
        DateTime to, 
        string? username = null, 
        IDbTransaction? transaction = null)
    {
        var sql = @"SELECT * FROM AuditLogs 
                    WHERE Timestamp >= @From AND Timestamp <= @To";
        
        if (!string.IsNullOrEmpty(username))
        {
            sql += " AND Username = @Username";
        }
        
        sql += " ORDER BY Timestamp DESC";
        
        return await _connection.QueryAsync<AuditLog>(sql, 
            new { From = from, To = to, Username = username }, 
            transaction);
    }

    public async Task<IEnumerable<AuditLog>> GetFailedActionsAsync(
        DateTime from, 
        DateTime to, 
        IDbTransaction? transaction = null)
    {
        var sql = @"SELECT * FROM AuditLogs 
                    WHERE Timestamp >= @From 
                    AND Timestamp <= @To 
                    AND IsSuccess = 0
                    ORDER BY Timestamp DESC";
        
        return await _connection.QueryAsync<AuditLog>(sql, new { From = from, To = to }, transaction);
    }

    public async Task<IEnumerable<AuditLog>> GetHighRiskActionsAsync(
        DateTime from, 
        DateTime to, 
        IDbTransaction? transaction = null)
    {
        var sql = @"SELECT * FROM AuditLogs 
                    WHERE Timestamp >= @From 
                    AND Timestamp <= @To 
                    AND RiskLevel IN ('High', 'Critical')
                    ORDER BY Timestamp DESC";
        
        return await _connection.QueryAsync<AuditLog>(sql, new { From = from, To = to }, transaction);
    }
}
