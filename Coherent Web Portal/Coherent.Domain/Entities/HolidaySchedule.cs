namespace Coherent.Domain.Entities;

/// <summary>
/// Holiday schedule entity from UEMedical_For_R&D database
/// </summary>
public class HolidaySchedule
{
    public long HId { get; set; }
    public string? Years { get; set; } // Format: YYYY
    public string? MonthDay { get; set; } // Format: MMDD
    public string? StartingTime { get; set; } // Format: HHMM
    public string? EndingTime { get; set; } // Format: HHMM
    public long SiteID { get; set; } // -1 means all sites
    public bool IsHoliday { get; set; }
    public bool IsActive { get; set; }
    public string? HolidayName { get; set; }
}
