namespace Coherent.Core.DTOs;

public class CRMUserListRequest
{
    public int? EmpType { get; set; }
    public bool? IsCRM { get; set; }
    public int Limit { get; set; } = 100;
}

public class CRMUserDto
{
    public long EmpId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? FName { get; set; }
    public string? LName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? UserName { get; set; }
    public int EmpType { get; set; }
    public string EmpTypeName { get; set; } = string.Empty;
    public string? Speciality { get; set; }
    public int? DepartmentID { get; set; }
    public bool IsCRM { get; set; }
    public bool Active { get; set; }
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
}

public class CRMUserListResponse
{
    public List<CRMUserDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
}

public class UpdateIsCRMRequest
{
    public long EmpId { get; set; }
    public bool IsCRM { get; set; }
}

public class BulkUpdateIsCRMRequest
{
    public List<long> EmpIds { get; set; } = new();
    public bool IsCRM { get; set; }
}

public class UpdateIsCRMResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AffectedCount { get; set; }
}

public class UpdateRoleIdRequest
{
    public int? RoleId { get; set; }
}

public class UpdateRoleIdResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
