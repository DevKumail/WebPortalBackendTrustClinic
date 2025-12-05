namespace Coherent.Domain.Entities;

/// <summary>
/// Blocked timeslot entity from UEMedical_For_R&D database
/// </summary>
public class SchBlockTimeslot
{
    public long BlockId { get; set; }
    public long ProviderId { get; set; }
    public long? SiteId { get; set; }
    public string? EffectiveDateTime { get; set; } // Format: YYYYMMDDHHMMSS
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedDate { get; set; }
}
