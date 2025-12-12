namespace Coherent.Domain.Entities;

/// <summary>
/// HR Employee entity (includes doctors/providers) from UEMedical_For_R&D database
/// </summary>
public class HREmployee
{
    public long EmpId { get; set; }
    public string? FName { get; set; }
    public string? MName { get; set; }
    public string? LName { get; set; }
    public string? Prefix { get; set; }
    public string? ProvNPI { get; set; } // Provider NPI/Alias
    public int EmpType { get; set; } // 1=Provider/Doctor
    public bool Active { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Speciality { get; set; }
    public string? UserName { get; set; }
    public string? Pass { get; set; }
    public int? RoleId { get; set; }
    public bool VIPPatientAccess { get; set; }
    public int? DepartmentID { get; set; }
}
