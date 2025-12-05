namespace Coherent.Domain.Entities;

/// <summary>
/// Provider schedule entity from UEMedical_For_R&D database
/// Used for managing doctor appointment schedules
/// </summary>
public class ProviderSchedule
{
    public long PSId { get; set; }
    public long ProviderId { get; set; }
    public long SiteId { get; set; }
    public long? UsageId { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public long Days { get; set; } // Bit flags: Mon=1, Tue=2, Wed=4, Thu=8, Fri=16, Sat=32, Sun=64
    public string? StartDate { get; set; } // Format: YYYYMMDDHHMMSS
    public string? EndDate { get; set; } // Format: YYYYMMDDHHMMSS
    public string? BreakStartTime { get; set; }
    public string? BreakEndTime { get; set; }
    public string? BreakReason { get; set; }
    public int? AppPerHour { get; set; }
    public int? MaxOverloadApps { get; set; }
    public int Priority { get; set; }
    public bool Active { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public string? UpdatedDate { get; set; }
}
