using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

/// <summary>
/// Third-party client repository for managing external system integrations
/// </summary>
public class ThirdPartyClientRepository : BaseRepository<ThirdPartyClient>
{
    protected override string TableName => "ThirdPartyClients";

    public ThirdPartyClientRepository(IDbConnection connection) : base(connection)
    {
    }

    public async Task<ThirdPartyClient?> GetByClientIdAsync(string clientId, IDbTransaction? transaction = null)
    {
        var sql = @"SELECT * FROM ThirdPartyClients 
                    WHERE ClientId = @ClientId AND IsActive = 1";
        return await _connection.QueryFirstOrDefaultAsync<ThirdPartyClient>(sql, 
            new { ClientId = clientId }, transaction);
    }

    public async Task<ThirdPartyClient?> GetBySecurityKeyAsync(string securityKey, IDbTransaction? transaction = null)
    {
        var sql = @"SELECT * FROM ThirdPartyClients 
                    WHERE SecurityKey = @SecurityKey";
        return await _connection.QueryFirstOrDefaultAsync<ThirdPartyClient>(sql, 
            new { SecurityKey = securityKey }, transaction);
    }

    public async Task<bool> UpdateLastAccessAsync(Guid id, IDbTransaction? transaction = null)
    {
        var sql = @"UPDATE ThirdPartyClients 
                    SET LastAccessAt = @LastAccess 
                    WHERE Id = @Id";
        var result = await _connection.ExecuteAsync(sql, 
            new { Id = id, LastAccess = DateTime.UtcNow }, transaction);
        return result > 0;
    }

    public async Task<bool> IsIpWhitelistedAsync(Guid clientId, string ipAddress, IDbTransaction? transaction = null)
    {
        var sql = @"SELECT IpWhitelist FROM ThirdPartyClients 
                    WHERE Id = @ClientId AND IsActive = 1";
        var whitelist = await _connection.QueryFirstOrDefaultAsync<string>(sql, 
            new { ClientId = clientId }, transaction);
        
        if (string.IsNullOrEmpty(whitelist))
            return false;

        var allowedIps = whitelist.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(ip => ip.Trim());
        
        return allowedIps.Contains(ipAddress) || allowedIps.Contains("*");
    }
}
