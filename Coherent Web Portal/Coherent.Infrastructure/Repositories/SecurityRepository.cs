using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class SecurityRepository : ISecurityRepository
{
    private readonly IDbConnection _connection;
    private readonly ILogger<SecurityRepository> _logger;

    public SecurityRepository(IDbConnection connection, ILogger<SecurityRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<List<SecRoleDto>> GetRolesAsync()
    {
        try
        {
            var sql = @"SELECT RoleId, RoleName, Active, IsAdmin FROM SecRole WHERE Active = 1 ORDER BY RoleName";
            var rows = await _connection.QueryAsync<SecRoleDto>(sql);
            return rows.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRolesAsync");
            throw;
        }
    }

    public async Task<List<SecPermissionDto>> GetPermissionsAsync()
    {
        try
        {
            var sql = @"SELECT PermissionId, PermissionKey, Module, Description, IsActive FROM SecPermission WHERE IsActive = 1 ORDER BY Module, PermissionKey";
            var rows = await _connection.QueryAsync<SecPermissionDto>(sql);
            return rows.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPermissionsAsync");
            throw;
        }
    }

    public async Task<List<string>> GetRolePermissionKeysAsync(int roleId)
    {
        try
        {
            var sql = @"
SELECT p.PermissionKey
FROM SecRolePermission rp
INNER JOIN SecPermission p ON p.PermissionId = rp.PermissionId
WHERE rp.RoleId = @RoleId AND rp.IsAllowed = 1 AND p.IsActive = 1
ORDER BY p.PermissionKey";

            var rows = await _connection.QueryAsync<string>(sql, new { RoleId = roleId });
            return rows.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRolePermissionKeysAsync for RoleId={RoleId}", roleId);
            throw;
        }
    }

    public async Task SetRolePermissionsAsync(int roleId, List<string> permissionKeys, string? assignedBy)
    {
        if (permissionKeys == null)
            throw new ArgumentNullException(nameof(permissionKeys));

        try
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            using var tx = _connection.BeginTransaction();

            await _connection.ExecuteAsync(
                "DELETE FROM SecRolePermission WHERE RoleId = @RoleId",
                new { RoleId = roleId },
                tx);

            var insertSql = @"
INSERT INTO SecRolePermission (RoleId, PermissionId, IsAllowed, AssignedAt, AssignedBy)
SELECT @RoleId, p.PermissionId, 1, SYSUTCDATETIME(), @AssignedBy
FROM SecPermission p
WHERE p.IsActive = 1 AND p.PermissionKey = @PermissionKey";

            foreach (var key in permissionKeys.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await _connection.ExecuteAsync(insertSql, new { RoleId = roleId, PermissionKey = key, AssignedBy = assignedBy }, tx);
            }

            tx.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SetRolePermissionsAsync for RoleId={RoleId}", roleId);
            throw;
        }
    }

    public async Task<List<SecEmployeePermissionDto>> GetEmployeeEffectivePermissionsByEmpIdAsync(long empId)
    {
        try
        {
            var roleId = await _connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT RoleId FROM HREmployee WHERE EmpId = @EmpId AND Active = 1",
                new { EmpId = empId });

            var sql = @"
WITH RolePerms AS (
    SELECT p.PermissionKey, CAST(1 AS bit) AS IsAllowed, CAST(0 AS bit) AS IsOverride
    FROM SecRolePermission rp
    INNER JOIN SecPermission p ON p.PermissionId = rp.PermissionId
    WHERE rp.RoleId = @RoleId AND rp.IsAllowed = 1 AND p.IsActive = 1
), Overrides AS (
    SELECT p.PermissionKey, o.IsAllowed, CAST(1 AS bit) AS IsOverride
    FROM SecEmployeePermissionOverride o
    INNER JOIN SecPermission p ON p.PermissionId = o.PermissionId
    WHERE o.EmpId = @EmpId AND p.IsActive = 1
), Combined AS (
    SELECT PermissionKey, IsAllowed, IsOverride
    FROM Overrides
    UNION ALL
    SELECT PermissionKey, IsAllowed, IsOverride
    FROM RolePerms
    WHERE PermissionKey NOT IN (SELECT PermissionKey FROM Overrides)
)
SELECT p.PermissionKey,
       COALESCE(c.IsAllowed, CAST(0 AS bit)) AS IsAllowed,
       COALESCE(c.IsOverride, CAST(0 AS bit)) AS IsOverride
FROM SecPermission p
LEFT JOIN Combined c ON c.PermissionKey = p.PermissionKey
WHERE p.IsActive = 1
ORDER BY p.Module, p.PermissionKey";

            var rows = await _connection.QueryAsync<SecEmployeePermissionDto>(sql, new { EmpId = empId, RoleId = roleId });
            return rows.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetEmployeeEffectivePermissionsByEmpIdAsync for EmpId={EmpId}", empId);
            throw;
        }
    }

    public async Task<List<SecEmployeePermissionDto>> GetEmployeeEffectivePermissionsByUsernameAsync(string username)
    {
        try
        {
            var empId = await _connection.QueryFirstOrDefaultAsync<long?>(
                "SELECT EmpId FROM HREmployee WHERE UserName = @Username AND Active = 1",
                new { Username = username });

            if (empId == null)
                return new List<SecEmployeePermissionDto>();

            return await GetEmployeeEffectivePermissionsByEmpIdAsync(empId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetEmployeeEffectivePermissionsByUsernameAsync for Username={Username}", username);
            throw;
        }
    }

    public async Task SetEmployeePermissionOverridesAsync(long empId, List<SecEmployeePermissionOverrideDto> overrides, string? assignedBy)
    {
        if (overrides == null)
            throw new ArgumentNullException(nameof(overrides));

        try
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            using var tx = _connection.BeginTransaction();

            await _connection.ExecuteAsync(
                "DELETE FROM SecEmployeePermissionOverride WHERE EmpId = @EmpId",
                new { EmpId = empId },
                tx);

            var insertSql = @"
INSERT INTO SecEmployeePermissionOverride (EmpId, PermissionId, IsAllowed, AssignedAt, AssignedBy)
SELECT @EmpId, p.PermissionId, @IsAllowed, SYSUTCDATETIME(), @AssignedBy
FROM SecPermission p
WHERE p.IsActive = 1 AND p.PermissionKey = @PermissionKey";

            foreach (var o in overrides)
            {
                if (string.IsNullOrWhiteSpace(o.PermissionKey))
                    continue;

                await _connection.ExecuteAsync(
                    insertSql,
                    new { EmpId = empId, PermissionKey = o.PermissionKey, IsAllowed = o.IsAllowed, AssignedBy = assignedBy },
                    tx);
            }

            tx.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SetEmployeePermissionOverridesAsync for EmpId={EmpId}", empId);
            throw;
        }
    }

    public async Task<string?> GetRoleNameByRoleIdAsync(int roleId)
    {
        try
        {
            var sql = "SELECT TOP 1 RoleName FROM SecRole WHERE RoleId = @RoleId AND Active = 1";
            return await _connection.QueryFirstOrDefaultAsync<string?>(sql, new { RoleId = roleId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRoleNameByRoleIdAsync for RoleId={RoleId}", roleId);
            throw;
        }
    }
}
