using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using System.Globalization;

namespace Coherent.Infrastructure.Services;

public class AppointmentSchedulingService : IAppointmentSchedulingService
{
    private readonly IAppointmentSchedulingRepository _repository;

    public AppointmentSchedulingService(IAppointmentSchedulingRepository repository)
    {
        _repository = repository;
    }

    public async Task<AppointmentAvailabilityResponseDto> CheckAvailabilityAsync(
        long providerId,
        int locationTypeId,
        string appDateTime,
        int durationMinutes,
        bool allowOverBooking,
        bool isUMC = false)
    {
        if (providerId <= 0)
            throw new ArgumentException("providerId must be greater than 0", nameof(providerId));

        if (string.IsNullOrWhiteSpace(appDateTime))
            throw new ArgumentException("appDateTime is required", nameof(appDateTime));

        if (durationMinutes <= 0)
            throw new ArgumentException("durationMinutes must be greater than 0", nameof(durationMinutes));

        // Legacy passes date portion for FromDate/ToDate.
        var appDate = ExtractLegacyDatePortion(appDateTime);

        var existing = await _repository.LoadAppointmentsForAvailabilityAsync(providerId, locationTypeId, appDate, isUMC);

        var overlappedCount = 0;
        string? firstOverlapSiteId = null;

        var newStart = ParseLegacyDateTime(appDateTime);
        var newEnd = newStart.AddMinutes(durationMinutes);

        foreach (var apt in existing)
        {
            // Business rule: only check against AppStatusId == 1.
            if (apt.AppStatusId != 1)
                continue;

            if (string.IsNullOrWhiteSpace(apt.AppDateTime))
                continue;

            var existingStart = ParseLegacyDateTime(apt.AppDateTime);
            var existingEnd = existingStart.AddMinutes(apt.Duration);

            if (HasOverlap(newStart, newEnd, existingStart, existingEnd))
            {
                overlappedCount++;
                if (firstOverlapSiteId == null)
                    firstOverlapSiteId = apt.SiteId.ToString(CultureInfo.InvariantCulture);
            }
        }

        if (overlappedCount > 0 && !allowOverBooking)
        {
            return new AppointmentAvailabilityResponseDto
            {
                IsAllowed = false,
                ResultMessage = "You are not authorized to overbook an appointment",
                ShouldAskUserToConfirm = false,
                SiteId = firstOverlapSiteId,
                CountOverlappedAppointments = overlappedCount
            };
        }

        if (overlappedCount > 0 && allowOverBooking)
        {
            return new AppointmentAvailabilityResponseDto
            {
                IsAllowed = true,
                ResultMessage = null,
                ShouldAskUserToConfirm = true,
                SiteId = firstOverlapSiteId,
                CountOverlappedAppointments = overlappedCount
            };
        }

        return new AppointmentAvailabilityResponseDto
        {
            IsAllowed = true,
            ResultMessage = null,
            ShouldAskUserToConfirm = false,
            SiteId = null,
            CountOverlappedAppointments = 0
        };
    }

    public async Task<long> BookAppointmentAsync(BookAppointmentRequestV2 request)
    {
        if (request?.Appointment == null)
            throw new ArgumentException("appointment is required", nameof(request));

        var appointment = request.Appointment;

        // Legacy insert
        var newId = await _repository.InsertAppointmentAsync(appointment);

        // Procedures
        foreach (var p in request.Procedures)
        {
            p.AppId = newId;

            if (p.AppProcedureId <= 0)
            {
                p.AppProcedureId = await _repository.InsertAppointmentProcedureAsync(p);
            }
            else
            {
                await _repository.UpdateAppointmentProcedureAsync(p);
            }
        }

        // Deleted procedures by detail id
        foreach (var detailId in request.DeletedProcedureOrderDetailIds)
        {
            await _repository.DeleteAppointmentProcedureByDetailIdAsync(detailId);
        }

        // Optional patient notify update via PN SP (legacy has separate call)
        await _repository.UpdatePatientNotifyAsync(newId, appointment.IsPatientNotified, appointment.PatientNotifiedID);

        return newId;
    }

    public async Task<bool> UpdateAppointmentAsync(long appId, UpdateAppointmentRequestV2 request)
    {
        if (request?.Appointment == null)
            throw new ArgumentException("appointment is required", nameof(request));

        request.Appointment.AppId = appId;

        var ok = await _repository.UpdateAppointmentAsync(request.Appointment);
        if (!ok)
            return false;

        foreach (var p in request.Procedures)
        {
            p.AppId = appId;

            if (p.AppProcedureId <= 0)
            {
                p.AppProcedureId = await _repository.InsertAppointmentProcedureAsync(p);
            }
            else
            {
                await _repository.UpdateAppointmentProcedureAsync(p);
            }
        }

        foreach (var detailId in request.DeletedProcedureOrderDetailIds)
        {
            await _repository.DeleteAppointmentProcedureByDetailIdAsync(detailId);
        }

        await _repository.UpdatePatientNotifyAsync(appId, request.Appointment.IsPatientNotified, request.Appointment.PatientNotifiedID);

        return true;
    }

    public async Task<RescheduleAppointmentResponseV2> RescheduleAppointmentAsync(long appId, RescheduleAppointmentRequestV2 request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.OldAppointmentId != appId)
            request.OldAppointmentId = appId;

        // Insert NEW appointment
        var newAppId = await _repository.InsertAppointmentAsync(request.NewAppointment);

        foreach (var p in request.Procedures)
        {
            p.AppId = newAppId;
            if (p.AppProcedureId <= 0)
                p.AppProcedureId = await _repository.InsertAppointmentProcedureAsync(p);
            else
                await _repository.UpdateAppointmentProcedureAsync(p);
        }

        // Update OLD appointment status (legacy uses SchAppointmentUpdateAppStatus)
        // If caller doesn't specify, default is 2 (commonly used as "rescheduled" in portal).
        var oldStatusId = request.OldAppointmentStatusId ?? 2;
        var oldUpdated = await _repository.UpdateAppointmentStatusAsync(request.OldAppointmentId, oldStatusId, request.NewAppointment.ByProvider, request.NewAppointment.RescheduleID);
        if (!oldUpdated)
            throw new InvalidOperationException("Failed to update old appointment status");

        return new RescheduleAppointmentResponseV2
        {
            OldAppointmentId = request.OldAppointmentId,
            OldAppointmentStatusId = oldStatusId,
            NewAppointmentId = newAppId
        };
    }

    public async Task<CancelAppointmentResponseV2> CancelAppointmentAsync(long appId, CancelAppointmentRequestV2 request)
    {
        // Set status 3 = Cancel
        var ok = await _repository.UpdateAppointmentStatusAsync(appId, 3, request?.ByProvider ?? false, 0);

        if (ok && request != null && request.PatientNotifiedID.HasValue)
            await _repository.UpdatePatientNotifyAsync(appId, request.IsPatientNotified, request.PatientNotifiedID.Value);

        return new CancelAppointmentResponseV2
        {
            AppointmentId = appId,
            AppStatusId = 3,
            Success = ok
        };
    }

    private static bool HasOverlap(DateTime newStart, DateTime newEnd, DateTime existingStart, DateTime existingEnd)
    {
        // Three legacy overlap cases are equivalent to standard interval overlap.
        return newStart < existingEnd && newEnd > existingStart;
    }

    private static string ExtractLegacyDatePortion(string appDateTime)
    {
        // Supports "yyyyMMddHHmmss" and also ISO-ish strings.
        // For ISO, we'll parse and reformat to yyyyMMdd.
        if (appDateTime.Length >= 8 && long.TryParse(appDateTime.Substring(0, 8), out _))
            return appDateTime.Substring(0, 8);

        var parsed = DateTime.Parse(appDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
        return parsed.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }

    private static DateTime ParseLegacyDateTime(string value)
    {
        // Prefer legacy 14 char string
        if (value.Length >= 14 && long.TryParse(value.Substring(0, 14), out _))
        {
            var year = int.Parse(value.Substring(0, 4), CultureInfo.InvariantCulture);
            var month = int.Parse(value.Substring(4, 2), CultureInfo.InvariantCulture);
            var day = int.Parse(value.Substring(6, 2), CultureInfo.InvariantCulture);
            var hour = int.Parse(value.Substring(8, 2), CultureInfo.InvariantCulture);
            var min = int.Parse(value.Substring(10, 2), CultureInfo.InvariantCulture);
            var sec = int.Parse(value.Substring(12, 2), CultureInfo.InvariantCulture);
            return new DateTime(year, month, day, hour, min, sec);
        }

        return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
    }
}
