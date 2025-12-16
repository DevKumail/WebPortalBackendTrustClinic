using System.Text.Json.Serialization;

namespace Coherent.Core.DTOs;

public class SecRoleDto
{
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
    public bool Active { get; set; }
    public bool? IsAdmin { get; set; }
}

public class SecPermissionDto
{
    public int PermissionId { get; set; }
    public string? PermissionKey { get; set; }
    public string? Module { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class SetRolePermissionsRequest
{
    public List<string> PermissionKeys { get; set; } = new();
}

public class SecEmployeePermissionDto
{
    public string PermissionKey { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
    public bool IsOverride { get; set; }
}

public class SecEmployeePermissionOverrideDto
{
    [JsonPropertyName("permissionKey")]
    public string PermissionKey { get; set; } = string.Empty;

    [JsonPropertyName("isAllowed")]
    public bool IsAllowed { get; set; }
}

public class SetEmployeePermissionOverridesRequest
{
    public List<SecEmployeePermissionOverrideDto> Overrides { get; set; } = new();
}
