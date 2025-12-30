namespace Coherent.Core.DTOs;

/// <summary>
/// Patient list item DTO with essential fields
/// </summary>
public class PatientListItemDto
{
    public string? MRNo { get; set; }
    public string? PersonFirstName { get; set; }
    public string? PersonMiddleName { get; set; }
    public string? PersonLastName { get; set; }
    public string? FullName => $"{PersonFirstName} {PersonMiddleName} {PersonLastName}".Trim();
    public string? PersonSex { get; set; }
    public DateTime? PatientBirthDate { get; set; }
    public string? PatientBirthDateString { get; set; } // Original string from DB
    public int? Age
    {
        get
        {
            if (PatientBirthDate.HasValue)
            {
                var today = DateTime.Today;
                var age = today.Year - PatientBirthDate.Value.Year;
                if (PatientBirthDate.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
            return null;
        }
    }
    public string? PersonCellPhone { get; set; }
    public string? PersonEmail { get; set; }
    public string? PersonAddress1 { get; set; }
    public string? Nationality { get; set; }
    public string? PersonSocialSecurityNo { get; set; }
    public DateTime? PatientFirstVisitDate { get; set; }
    public string? PatientFirstVisitDateString { get; set; } // Original string from DB
    public DateTime? CreatedDate { get; set; }
    public string? CreatedDateString { get; set; } // Original string from DB
    public bool? VIPPatient { get; set; }
    public string? FacilityName { get; set; }
    public bool? IsMobileUser { get; set; } // On-boarded on Mobile App: Yes/No
    
    /// <summary>
    /// Helper method to convert database date string (YYYYMMDDHHMMSS) to DateTime
    /// </summary>
    public static DateTime? ParseDbDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString) || dateString.Length < 8)
            return null;

        try
        {
            // Handle format: YYYYMMDDHHMMSS or YYYYMMDD
            if (dateString.Length >= 14)
            {
                return DateTime.ParseExact(dateString.Substring(0, 14), "yyyyMMddHHmmss", null);
            }
            else if (dateString.Length >= 8)
            {
                return DateTime.ParseExact(dateString.Substring(0, 8), "yyyyMMdd", null);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Paginated patient list response
/// </summary>
public class PaginatedPatientResponse
{
    public List<PatientListItemDto> Patients { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}

/// <summary>
/// Patient search request parameters
/// </summary>
public class PatientSearchRequest
{
    public string? MRNo { get; set; }
    public string? Name { get; set; }
    public string? PersonSocialSecurityNo { get; set; }
    public string? CellPhone { get; set; }
    /// <summary>
    /// Visit date range start (default: current date - 31 days)
    /// </summary>
    public DateTime? VisitDateFrom { get; set; }
    /// <summary>
    /// Visit date range end (default: today)
    /// </summary>
    public DateTime? VisitDateTo { get; set; }
    /// <summary>
    /// Filter by mobile app onboarding status: true = Yes, false = No, null = Both
    /// </summary>
    public bool? OnboardedOnMobileApp { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
