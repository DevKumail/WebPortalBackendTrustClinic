using System.Text.Json.Serialization;

namespace Coherent.Core.DTOs;

public class SchAppointmentDto
{
    public long AppId { get; set; }
    public long ProviderId { get; set; }
    public string? MRNo { get; set; }
    public string? AppDateTime { get; set; }
    public int Duration { get; set; }
    public string? AppNote { get; set; }
    public int SiteId { get; set; }
    public int LocationId { get; set; }
    public int AppTypeId { get; set; }
    public int AppCriteriaId { get; set; }
    public int AppStatusId { get; set; }
    public int PatientStatusId { get; set; }
    public long? ReferredProviderId { get; set; }
    public bool IsPatientNotified { get; set; }
    public bool IsActive { get; set; }
    public string? EnteredBy { get; set; }
    public string? EntryDateTime { get; set; }
    public string? PurposeOfVisit { get; set; }
    public int PatientNotifiedID { get; set; }
    public int RescheduleID { get; set; }
    public bool ByProvider { get; set; }
    public int SpecialtyId { get; set; }
    public bool UpdateServerTime { get; set; }
    public bool VisitStatusEnabled { get; set; }
    public long Anesthesiologist { get; set; }
    public long CPTGroupId { get; set; }
    public int AppointmentClassification { get; set; }
    public long OrderReferralId { get; set; }
    public string? TelemedicineURL { get; set; }

    public string? DateTimeNotYetArrived { get; set; }
    public string? DateTimeCheckIn { get; set; }
    public string? DateTimeReady { get; set; }
    public string? DateTimeSeen { get; set; }
    public string? DateTimeBilled { get; set; }
    public string? DateTimeCheckOut { get; set; }

    public string? UserNotYetArrived { get; set; }
    public string? UserCheckIn { get; set; }
    public string? UserReady { get; set; }
    public string? UserSeen { get; set; }
    public string? UserBilled { get; set; }
    public string? UserCheckOut { get; set; }
}

public class SchAppointmentProcedureDto
{
    public long AppProcedureId { get; set; }
    public long AppId { get; set; }
    public string? ProcedureCode { get; set; }
    public string? ProcedureName { get; set; }
    public int LocationID { get; set; }
    public string? StartTime { get; set; }
    public int Duration { get; set; }
    public bool Active { get; set; }
    public long OrderDetailId { get; set; }
}

public class AppointmentAvailabilityResponseDto
{
    public bool IsAllowed { get; set; }
    public string? ResultMessage { get; set; }
    public bool ShouldAskUserToConfirm { get; set; }
    public string? SiteId { get; set; }
    public int CountOverlappedAppointments { get; set; }
}

public class BookAppointmentRequestV2
{
    public SchAppointmentDto Appointment { get; set; } = new();
    public List<SchAppointmentProcedureDto> Procedures { get; set; } = new();

    public List<long> DeletedProcedureOrderDetailIds { get; set; } = new();

    public string? PatientNotifyString { get; set; }
}

public class UpdateAppointmentRequestV2
{
    public SchAppointmentDto Appointment { get; set; } = new();
    public List<SchAppointmentProcedureDto> Procedures { get; set; } = new();
    public List<long> DeletedProcedureOrderDetailIds { get; set; } = new();
    public string? PatientNotifyString { get; set; }
}

public class RescheduleAppointmentRequestV2
{
    public long OldAppointmentId { get; set; }
    public int? OldAppointmentStatusId { get; set; }
    public SchAppointmentDto NewAppointment { get; set; } = new();
    public List<SchAppointmentProcedureDto> Procedures { get; set; } = new();
}

public class RescheduleAppointmentResponseV2
{
    public long OldAppointmentId { get; set; }
    public int OldAppointmentStatusId { get; set; }
    public long NewAppointmentId { get; set; }
}

public class CancelAppointmentRequestV2
{
    public string? CancelReason { get; set; }
    public string? PatientNotifyString { get; set; }
    public bool CancelOrders { get; set; }
    public bool ByProvider { get; set; }
    public bool IsPatientNotified { get; set; }
    public int? PatientNotifiedID { get; set; }
}

public class CancelAppointmentResponseV2
{
    public long AppointmentId { get; set; }
    public int AppStatusId { get; set; }
    public bool Success { get; set; }
}
