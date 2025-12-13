using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

public interface IAppointmentSchedulingRepository
{
    Task<List<SchAppointmentDto>> LoadAppointmentsForAvailabilityAsync(
        long providerId,
        int locationTypeId,
        string appDate,
        bool isUMC);

    Task<long> InsertAppointmentAsync(SchAppointmentDto appointment);
    Task<bool> UpdateAppointmentAsync(SchAppointmentDto appointment);

    Task<bool> UpdateAppointmentStatusAsync(long appId, int appStatusId, bool byProvider, int rescheduledId);

    Task<bool> UpdatePatientNotifyAsync(long appId, bool isPatientNotified, int patientNotifiedId);

    Task<long> InsertAppointmentProcedureAsync(SchAppointmentProcedureDto procedure);
    Task<bool> UpdateAppointmentProcedureAsync(SchAppointmentProcedureDto procedure);
    Task<bool> DeleteAppointmentProcedureByDetailIdAsync(long orderDetailId);
}
