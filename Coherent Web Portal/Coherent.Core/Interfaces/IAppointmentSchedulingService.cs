using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

public interface IAppointmentSchedulingService
{
    Task<AppointmentAvailabilityResponseDto> CheckAvailabilityAsync(
        long providerId,
        int locationTypeId,
        string appDateTime,
        int durationMinutes,
        bool allowOverBooking,
        bool isUMC = false);

    Task<long> BookAppointmentAsync(BookAppointmentRequestV2 request);
    Task<bool> UpdateAppointmentAsync(long appId, UpdateAppointmentRequestV2 request);
    Task<RescheduleAppointmentResponseV2> RescheduleAppointmentAsync(long appId, RescheduleAppointmentRequestV2 request);
    Task<CancelAppointmentResponseV2> CancelAppointmentAsync(long appId, CancelAppointmentRequestV2 request);
}
