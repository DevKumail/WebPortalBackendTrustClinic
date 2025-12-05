using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

/// <summary>
/// Repository for logging all third-party requests (ADHICS compliance)
/// </summary>
public class ThirdPartyRequestLogRepository : BaseRepository<ThirdPartyRequestLog>
{
    protected override string TableName => "ThirdPartyRequestLogs";

    public ThirdPartyRequestLogRepository(IDbConnection connection) : base(connection)
    {
    }

    public async Task<int> GetRequestCountInLastMinuteAsync(Guid clientId, IDbTransaction? transaction = null)
    {
        var sql = @"SELECT COUNT(*) FROM ThirdPartyRequestLogs 
                    WHERE ThirdPartyClientId = @ClientId 
                    AND RequestTimestamp >= @MinuteAgo";
        
        var minuteAgo = DateTime.UtcNow.AddMinutes(-1);
        return await _connection.ExecuteScalarAsync<int>(sql, 
            new { ClientId = clientId, MinuteAgo = minuteAgo }, transaction);
    }

    public async Task<IEnumerable<ThirdPartyRequestLog>> GetFailedRequestsAsync(
        Guid clientId, 
        DateTime from, 
        DateTime to, 
        IDbTransaction? transaction = null)
    {
        var sql = @"SELECT * FROM ThirdPartyRequestLogs 
                    WHERE ThirdPartyClientId = @ClientId 
                    AND IsSuccess = 0
                    AND RequestTimestamp >= @From 
                    AND RequestTimestamp <= @To
                    ORDER BY RequestTimestamp DESC";
        
        return await _connection.QueryAsync<ThirdPartyRequestLog>(sql, 
            new { ClientId = clientId, From = from, To = to }, transaction);
    }
}
