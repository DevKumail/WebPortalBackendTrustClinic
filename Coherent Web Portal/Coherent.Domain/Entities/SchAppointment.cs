namespace Coherent.Domain.Entities;

/// <summary>
/// Appointment entity from UEMedical_For_R&D database
/// </summary>
public class SchAppointment
{
    public long AppId { get; set; }
    public string? MRNo { get; set; }
    public long ProviderId { get; set; }
    public long SiteId { get; set; }
    public string? AppDate { get; set; } // Format: YYYYMMDD
    public string? AppDateTime { get; set; } // Format: YYYYMMDDHHMMSS
    public int Duration { get; set; } // Duration in minutes
    public int AppStatusId { get; set; } // 1=Scheduled, 2=Rescheduled, 3=Cancelled
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public string? UpdatedDate { get; set; }
}
