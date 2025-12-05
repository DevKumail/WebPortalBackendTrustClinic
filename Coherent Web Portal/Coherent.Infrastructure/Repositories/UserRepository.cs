using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

/// <summary>
/// User repository with custom queries using parameterized Dapper queries
/// </summary>
public class UserRepository : BaseRepository<User>
{
    protected override string TableName => "Users";

    public UserRepository(IDbConnection connection) : base(connection)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username, IDbTransaction? transaction = null)
    {
        var sql = "SELECT * FROM Users WHERE Username = @Username";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username }, transaction);
    }

    public async Task<User?> GetByEmailAsync(string email, IDbTransaction? transaction = null)
    {
        var sql = "SELECT * FROM Users WHERE Email = @Email";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email }, transaction);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, IDbTransaction? transaction = null)
    {
        var sql = @"SELECT * FROM Users 
                    WHERE RefreshToken = @RefreshToken 
                    AND RefreshTokenExpiry > @CurrentTime";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, 
            new { RefreshToken = refreshToken, CurrentTime = DateTime.UtcNow }, transaction);
    }

    public async Task<bool> UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expiry, IDbTransaction? transaction = null)
    {
        var sql = @"UPDATE Users 
                    SET RefreshToken = @RefreshToken, 
                        RefreshTokenExpiry = @Expiry,
                        LastLoginAt = @LoginTime
                    WHERE Id = @UserId";
        var result = await _connection.ExecuteAsync(sql, 
            new { UserId = userId, RefreshToken = refreshToken, Expiry = expiry, LoginTime = DateTime.UtcNow }, 
            transaction);
        return result > 0;
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId, IDbTransaction? transaction = null)
    {
        var sql = @"SELECT r.Name 
                    FROM Roles r
                    INNER JOIN UserRoles ur ON r.Id = ur.RoleId
                    WHERE ur.UserId = @UserId AND r.IsActive = 1";
        return await _connection.QueryAsync<string>(sql, new { UserId = userId }, transaction);
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId, IDbTransaction? transaction = null)
    {
        var sql = @"SELECT DISTINCT p.Name
                    FROM Permissions p
                    INNER JOIN RolePermissions rp ON p.Id = rp.PermissionId
                    INNER JOIN UserRoles ur ON rp.RoleId = ur.RoleId
                    WHERE ur.UserId = @UserId";
        return await _connection.QueryAsync<string>(sql, new { UserId = userId }, transaction);
    }
}
