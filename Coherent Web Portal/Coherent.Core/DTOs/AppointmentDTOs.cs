namespace Coherent.Core.DTOs;

/// <summary>
/// Doctor available slot DTO
/// </summary>
public class DoctorAvailableSlotDto
{
    public string? SlotId { get; set; }
    public string? DttmFrom { get; set; } // Format: yyyy-MM-dd HH:mm:ss
    public string? DttmTo { get; set; } // Format: yyyy-MM-dd HH:mm:ss
    public string? DttmDuration { get; set; } // Duration in minutes
    public string? SlotState { get; set; } // ACTIVE, BOOKED, BLOCKED
    public string? SlotType { get; set; } // F=Free, B=Booked
    public string? UpdtDttm { get; set; }
}

/// <summary>
/// Doctor with available slots DTO
/// </summary>
public class DoctorSlotsDto
{
    public string? SpecialityId { get; set; }
    public string? SpecialityName { get; set; }
    public string? FacilityId { get; set; }
    public string? ResourceCd { get; set; }
    public string? PrsnlId { get; set; }
    public string? PrsnlName { get; set; }
    public string? ResourceName { get; set; }
    public string? PrsnlAlias { get; set; }
    public string? ExecDttmFrom { get; set; }
    public string? ExecDttmTo { get; set; }
    public List<DoctorAvailableSlotDto> AvailableSlots { get; set; } = new();
}

/// <summary>
/// Appointment DTO
/// </summary>
public class AppointmentDto
{
    public long AppId { get; set; }
    public string? MRNo { get; set; }
    public long DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public string? Speciality { get; set; }
    public long SiteId { get; set; }
    public string? SiteName { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public DateTime? AppointmentDateTime { get; set; }
    public int Duration { get; set; }
    public string? Status { get; set; } // Scheduled, Rescheduled, Cancelled
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedDate { get; set; }
}

/// <summary>
/// Book appointment request
/// </summary>
public class BookAppointmentRequest
{
    public long DoctorId { get; set; }
    public string? MRNO { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public string? Day { get; set; } // Monday, Tuesday, etc.
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Modify appointment request
/// </summary>
public class ModifyAppointmentRequest
{
    public long AppId { get; set; }
    public long DoctorId { get; set; }
    public string? MRNO { get; set; }
    public DateTime? AppointmentDateTime { get; set; }
    public string? Status { get; set; } // rescheduled, cancel
    public string? Reason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Get available slots request
/// </summary>
public class GetAvailableSlotsRequest
{
    
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? PrsnlAlias { get; set; } // Doctor NPI
}

/// <summary>
/// Doctor profile DTO
/// </summary>
public class DoctorProfileDto
{
    public int DId { get; set; }
    public string? DoctorName { get; set; }
    public string? ArDoctorName { get; set; }
    public string? Title { get; set; }
    public string? ArTitle { get; set; }
    public string? Speciality { get; set; }
    public string? ArSpeciality { get; set; }
    public string? YearsOfExperience { get; set; }
    public string? Nationality { get; set; }
    public string? Languages { get; set; }
    public string? DoctorPhotoName { get; set; }
    public string? About { get; set; }
    public string? Education { get; set; }
    public string? Experience { get; set; }
    public string? Expertise { get; set; }
    public string? LicenceNo { get; set; }
    public string? Gender { get; set; }
    public bool? Active { get; set; }
    public List<string> Facilities { get; set; } = new();
}

/// <summary>
/// Vital signs DTO
/// </summary>
public class VitalSignsDto
{
    public string? MRNO { get; set; }
    public decimal? Weight { get; set; } // in kg
    public decimal? Height { get; set; } // in cm
    public decimal? BMI { get; set; }
    public decimal? Temperature { get; set; } // in Celsius
    public string? BloodPressure { get; set; } // e.g., "120/80"
    public int? HeartRate { get; set; } // beats per minute
    public DateTime? RecordedDate { get; set; }
}

/// <summary>
/// Medication DTO
/// </summary>
public class MedicationDto
{
    public long MedicationId { get; set; }
    public string? MRNO { get; set; }
    public string? MedicationName { get; set; }
    public string? Dosage { get; set; }
    public string? Frequency { get; set; }
    public string? Route { get; set; } // Oral, IV, etc.
    public string? PrescribedBy { get; set; }
    public DateTime? PrescribedDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Instructions { get; set; }
    public bool? IsActive { get; set; }
}

 public class MedicationV2Dto
 {
     public long MedicationId { get; set; }
     public string? Mrno { get; set; }
     public long? VisitAccountNo { get; set; }
     public string? Rx { get; set; }
     public string? Dose { get; set; }
     public string? ProviderName { get; set; }
     public string? Route { get; set; }
     public string? Frequency { get; set; }
     public string? Duration { get; set; }
     public string? Quantity { get; set; }
     public string? PrescriptionDate { get; set; }
     public string? StartDate { get; set; }
     public string? StopDate { get; set; }
     public string? DaysLeft { get; set; }
     public string? ProviderImage { get; set; }
     public string? Instructions { get; set; }
     public string? Status { get; set; }
 }

/// <summary>
/// Allergy DTO
/// </summary>
public class AllergyDto
{
    public long AllergyId { get; set; }
    public string? MRNO { get; set; }
    public long? VisitAccountNo { get; set; }
    public int? TypeId { get; set; }
    public string? AllergyType { get; set; } // Drug, Food, Environmental
    public string? ViewAllergyTypeName { get; set; }
    public string? Allergen { get; set; }
    public string? Reaction { get; set; }
    public string? Severity { get; set; } // Mild, Moderate, Severe
    public DateTime? OnsetDate { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public int? Status { get; set; }
    public long? ProviderId { get; set; }
    public string? CreatedDate { get; set; }
    public string? SeverityCode { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

public class DiagnosisDto
{
    public long Id { get; set; }
    public long? VisitAccountNo { get; set; }
    public string? ICD9Code { get; set; }
    public bool? Confidential { get; set; }
    public string? LastUpdatedBy { get; set; }
    public string? LastUpdatedDate { get; set; }
    public string? MRNO { get; set; }
    public string? ICD9Description { get; set; }
    public long? ProviderId { get; set; }
    public string? VisitDate { get; set; }
    public string? DoctorName { get; set; }
    public string? Speciality { get; set; }
}
