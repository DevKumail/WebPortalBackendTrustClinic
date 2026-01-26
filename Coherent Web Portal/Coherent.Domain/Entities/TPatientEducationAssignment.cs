namespace Coherent.Domain.Entities;

public class TPatientEducationAssignment
{
    public int AssignmentId { get; set; }
    public int PatientId { get; set; }
    public int EducationId { get; set; }
    
    // Assignment details
    public int? AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; }
    public string? Notes { get; set; }
    public string? ArNotes { get; set; }
    
    // Patient viewing status
    public bool IsViewed { get; set; }
    public DateTime? ViewedAt { get; set; }
    
    // Expiry
    public DateTime? ExpiresAt { get; set; }
    
    // Status
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
