using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

public interface ISecurityRepository
{
    Task<List<SecRoleDto>> GetRolesAsync();
    Task<List<SecPermissionDto>> GetPermissionsAsync();

    Task<List<string>> GetRolePermissionKeysAsync(int roleId);
    Task SetRolePermissionsAsync(int roleId, List<string> permissionKeys, string? assignedBy);

    Task<List<SecEmployeePermissionDto>> GetEmployeeEffectivePermissionsByEmpIdAsync(long empId);
    Task<List<SecEmployeePermissionDto>> GetEmployeeEffectivePermissionsByUsernameAsync(string username);

    Task SetEmployeePermissionOverridesAsync(long empId, List<SecEmployeePermissionOverrideDto> overrides, string? assignedBy);

    Task<string?> GetRoleNameByRoleIdAsync(int roleId);
}
