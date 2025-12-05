using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Coherent.Infrastructure.Helpers;
using Dapper;
using System.Data;
using System.Globalization;

namespace Coherent.Infrastructure.Repositories;

/// <summary>
/// Repository for appointment and doctor slot management
/// Handles complex slot calculation logic from both databases
/// </summary>
public class AppointmentRepository : IAppointmentRepository
{
    private readonly IDbConnection _primaryConnection; // UEMedical_For_R&D
    private readonly IDbConnection _secondaryConnection; // CoherentMobApp

    public AppointmentRepository(IDbConnection primaryConnection, IDbConnection secondaryConnection)
    {
        _primaryConnection = primaryConnection;
        _secondaryConnection = secondaryConnection;
    }

    #region Get All Appointments by MRNO

    public async Task<List<AppointmentDto>> GetAllAppointmentsByMRNOAsync(string mrNo)
    {
        var query = @"
            SELECT 
                sa.AppId,
                sa.MRNo,
                sa.ProviderId AS DoctorId,
                he.FName + ' ' + he.MName + ' ' + he.LName AS DoctorName,
                sa.SiteId,
                sa.AppDate,
                sa.AppDateTime,
                sa.Duration,
                CASE sa.AppStatusId
                    WHEN 1 THEN 'Scheduled'
                    WHEN 2 THEN 'Rescheduled'
                    WHEN 3 THEN 'Cancelled'
                    ELSE 'Unknown'
                END AS Status,
                sa.Reason,
                sa.Notes,
                sa.CreatedDate
            FROM SchAppointments sa
            LEFT JOIN HREmployees he ON sa.ProviderId = he.EmpId
            WHERE sa.MRNo = @MRNo 
            AND sa.IsActive = 1
            ORDER BY sa.AppDateTime DESC";

        var appointments = await _primaryConnection.QueryAsync<AppointmentDto>(query, new { MRNo = mrNo });
        
        // Convert date strings to DateTime
        foreach (var apt in appointments)
        {
            if (!string.IsNullOrEmpty(apt.AppointmentDate?.ToString()))
            {
                apt.AppointmentDate = DateStringConversion.StringToDate(apt.AppointmentDate.ToString());
            }
            if (!string.IsNullOrEmpty(apt.AppointmentDateTime?.ToString()))
            {
                apt.AppointmentDateTime = DateStringConversion.StringToDate(apt.AppointmentDateTime.ToString());
            }
        }

        return appointments.ToList();
    }

    #endregion

    #region Book Appointment

    public async Task<long> BookAppointmentAsync(BookAppointmentRequest request)
    {
        var query = @"
            INSERT INTO SchAppointments 
            (MRNo, ProviderId, SiteId, AppDate, AppDateTime, Duration, AppStatusId, IsActive, Reason, Notes, CreatedDate, CreatedBy)
            VALUES 
            (@MRNo, @ProviderId, @SiteId, @AppDate, @AppDateTime, @Duration, 1, 1, @Reason, @Notes, @CreatedDate, 'System');
            
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        var appDate = DateStringConversion.DateToShortString(request.AppointmentDateTime);
        var appDateTime = DateStringConversion.DateToString(request.AppointmentDateTime);

        var parameters = new
        {
            MRNo = request.MRNO,
            ProviderId = request.DoctorId,
            SiteId = 1, // Default site, should be passed in request
            AppDate = appDate,
            AppDateTime = appDateTime,
            Duration = 15, // Default 15 minutes
            Reason = request.Reason,
            Notes = request.Notes,
            CreatedDate = DateStringConversion.DateToString(DateTime.Now)
        };

        return await _primaryConnection.ExecuteScalarAsync<long>(query, parameters);
    }

    #endregion

    #region Modify Appointment

    public async Task<bool> ModifyAppointmentAsync(ModifyAppointmentRequest request)
    {
        string query;
        object parameters;

        if (request.Status?.ToLower() == "cancel" || request.Status?.ToLower() == "cancelled")
        {
            // Cancel appointment
            query = @"
                UPDATE SchAppointments 
                SET AppStatusId = 3, 
                    IsActive = 0,
                    Reason = @Reason,
                    UpdatedDate = @UpdatedDate,
                    UpdatedBy = 'System'
                WHERE AppId = @AppId";

            parameters = new
            {
                AppId = request.AppId,
                Reason = request.Reason ?? "Cancelled by patient",
                UpdatedDate = DateStringConversion.DateToString(DateTime.Now)
            };
        }
        else if (request.Status?.ToLower() == "rescheduled" && request.AppointmentDateTime.HasValue)
        {
            // Reschedule appointment
            var appDate = DateStringConversion.DateToShortString(request.AppointmentDateTime.Value);
            var appDateTime = DateStringConversion.DateToString(request.AppointmentDateTime.Value);

            query = @"
                UPDATE SchAppointments 
                SET AppStatusId = 2,
                    AppDate = @AppDate,
                    AppDateTime = @AppDateTime,
                    Reason = @Reason,
                    Notes = @Notes,
                    UpdatedDate = @UpdatedDate,
                    UpdatedBy = 'System'
                WHERE AppId = @AppId";

            parameters = new
            {
                AppId = request.AppId,
                AppDate = appDate,
                AppDateTime = appDateTime,
                Reason = request.Reason ?? "Rescheduled",
                Notes = request.Notes,
                UpdatedDate = DateStringConversion.DateToString(DateTime.Now)
            };
        }
        else
        {
            return false;
        }

        var rowsAffected = await _primaryConnection.ExecuteAsync(query, parameters);
        return rowsAffected > 0;
    }

    #endregion

    #region Get Appointment By Id

    public async Task<SchAppointment?> GetAppointmentByIdAsync(long appId)
    {
        var query = "SELECT * FROM SchAppointments WHERE AppId = @AppId";
        return await _primaryConnection.QueryFirstOrDefaultAsync<SchAppointment>(query, new { AppId = appId });
    }

    #endregion

    #region Get Available Slots

    public async Task<List<DoctorSlotsDto>> GetAvailableSlotsOfDoctorAsync(GetAvailableSlotsRequest request)
    {
        var slots = new List<DoctorSlotsDto>();
        long slotId = 1000000; // Starting slot ID

        // Get provider info
        var provider = await _primaryConnection.QueryFirstOrDefaultAsync<HREmployee>(
            "SELECT * FROM HREmployees WHERE (ProvNPI = @Alias OR EmpId = @DoctorId) AND EmpType = 1 AND Active = 1",
            new { Alias = request.PrsnlAlias, DoctorId = request.DoctorId });

        if (provider == null)
            return slots;

        // Get provider schedules
        var schedules = await GetProviderSchedulesAsync(provider.EmpId, request.FromDate, request.ToDate);

        DateTime currentDate = request.FromDate;

        while (currentDate <= request.ToDate)
        {
            // Skip weekends
            if (DateStringConversion.IsWeekend(currentDate))
            {
                currentDate = currentDate.AddDays(1);
                continue;
            }

            string shortDate = DateStringConversion.DateToShortString(currentDate);
            string monthDay = currentDate.ToString("MMdd");

            // Filter schedules valid for current date
            var validSchedules = schedules.Where(ps =>
                (string.IsNullOrEmpty(ps.StartDate) || DateStringConversion.StringToDate(ps.StartDate).Date <= currentDate.Date) &&
                (string.IsNullOrEmpty(ps.EndDate) || DateStringConversion.StringToDate(ps.EndDate).Date >= currentDate.Date) &&
                DateStringConversion.DayExists(currentDate.DayOfWeek.ToString(), ps.Days)
            ).OrderBy(ps => ps.Priority).ToList();

            foreach (var schedule in validSchedules)
            {
                try
                {
                    // Parse start and end times
                    var startTime = DateTime.ParseExact(schedule.StartTime ?? "08:00 AM", "hh:mm tt", CultureInfo.InvariantCulture);
                    var endTime = DateTime.ParseExact(schedule.EndTime ?? "05:00 PM", "hh:mm tt", CultureInfo.InvariantCulture);
                    
                    DateTime timeSlotStart = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, startTime.Hour, startTime.Minute, 0);
                    DateTime timeSlotEnd = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, endTime.Hour, endTime.Minute, 0);

                    // Parse break times if any
                    DateTime? breakStart = null;
                    DateTime? breakEnd = null;
                    if (!string.IsNullOrEmpty(schedule.BreakStartTime))
                        breakStart = DateStringConversion.TimeStringToDate(currentDate, schedule.BreakStartTime);
                    if (!string.IsNullOrEmpty(schedule.BreakEndTime))
                        breakEnd = DateStringConversion.TimeStringToDate(currentDate, schedule.BreakEndTime);

                    // Generate 15-minute slots
                    var availableSlots = new List<DateTime>();
                    while (timeSlotStart < timeSlotEnd)
                    {
                        // Check if slot is not in break time
                        if ((breakStart == null || timeSlotStart < breakStart.Value || timeSlotStart >= breakEnd.Value))
                        {
                            availableSlots.Add(timeSlotStart);
                        }
                        timeSlotStart = timeSlotStart.AddMinutes(15);
                    }

                    // Remove holidays
                    var holidays = await GetHolidaysAsync(currentDate.Year, monthDay, schedule.SiteId);
                    foreach (var holiday in holidays)
                    {
                        if (string.IsNullOrEmpty(holiday.StartingTime) && string.IsNullOrEmpty(holiday.EndingTime))
                        {
                            availableSlots.RemoveAll(s => s.Date == currentDate.Date);
                        }
                        else
                        {
                            // Remove specific time range
                            // Implementation depends on holiday data format
                        }
                    }

                    // Remove blocked times
                    var blockedTimes = await GetBlockedTimeslotsAsync(provider.EmpId, shortDate, schedule.SiteId);
                    foreach (var blocked in blockedTimes)
                    {
                        var blockedTime = DateStringConversion.StringToDate(blocked.EffectiveDateTime);
                        availableSlots.RemoveAll(s => s == blockedTime);
                    }

                    // Remove existing appointments
                    var existingAppointments = await GetExistingAppointmentsAsync(provider.EmpId, shortDate, schedule.SiteId);
                    foreach (var apt in existingAppointments)
                    {
                        var aptTime = DateStringConversion.StringToDate(apt.AppDateTime);
                        int durationSlots = apt.Duration / 15;
                        for (int i = 0; i < durationSlots; i++)
                        {
                            availableSlots.RemoveAll(s => s == aptTime.AddMinutes(i * 15));
                        }
                    }

                    // Create DTO for available slots
                    if (availableSlots.Any())
                    {
                        var doctorSlot = new DoctorSlotsDto
                        {
                            PrsnlId = provider.EmpId.ToString(),
                            PrsnlName = $"{provider.Prefix} {provider.FName} {provider.MName} {provider.LName}".Trim(),
                            PrsnlAlias = provider.ProvNPI,
                            ResourceCd = provider.EmpId.ToString(),
                            ResourceName = $"{provider.FName} {provider.LName}",
                            SpecialityId = "1", // Should be fetched from ProviderSpecialtyAssign
                            SpecialityName = provider.Speciality,
                            FacilityId = "1", // Should be fetched from facility
                            ExecDttmFrom = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ExecDttmTo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            AvailableSlots = new List<DoctorAvailableSlotDto>()
                        };

                        foreach (var slot in availableSlots)
                        {
                            var slotDto = new DoctorAvailableSlotDto
                            {
                                SlotId = slotId.ToString(),
                                DttmFrom = slot.ToString("yyyy-MM-dd HH:mm:ss"),
                                DttmTo = slot.AddMinutes(15).ToString("yyyy-MM-dd HH:mm:ss"),
                                DttmDuration = "15",
                                SlotState = "ACTIVE",
                                SlotType = "F",
                                UpdtDttm = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                            };

                            doctorSlot.AvailableSlots.Add(slotDto);
                            slotId++;
                        }

                        slots.Add(doctorSlot);
                    }
                }
                catch (Exception ex)
                {
                    // Log error and continue
                    Console.WriteLine($"Error processing schedule: {ex.Message}");
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        return slots;
    }

    #endregion

    #region Get All Doctors

    public async Task<List<DoctorProfileDto>> GetAllDoctorsAsync()
    {
        var query = @"
            SELECT 
                d.DId,
                d.DoctorName,
                d.ArDoctorName,
                d.Title,
                d.ArTitle,
                s.SpecilityName AS Speciality,
                s.ArSpecilityName AS ArSpeciality,
                d.YearsOfExperience,
                d.Nationality,
                d.Languages,
                d.DoctorPhotoName,
                d.About,
                d.Education,
                d.Experience,
                d.Expertise,
                d.LicenceNo,
                d.Gender,
                d.Active
            FROM MDoctors d
            LEFT JOIN MSpecility s ON d.SPId = s.SPId
            WHERE d.Active = 1";

        var doctors = await _secondaryConnection.QueryAsync<DoctorProfileDto>(query);
        return doctors.ToList();
    }

    public async Task<DoctorProfileDto?> GetDoctorByIdAsync(int doctorId)
    {
        var query = @"
            SELECT 
                d.DId,
                d.DoctorName,
                d.ArDoctorName,
                d.Title,
                d.ArTitle,
                s.SpecilityName AS Speciality,
                s.ArSpecilityName AS ArSpeciality,
                d.YearsOfExperience,
                d.Nationality,
                d.Languages,
                d.DoctorPhotoName,
                d.About,
                d.Education,
                d.Experience,
                d.Expertise,
                d.LicenceNo,
                d.Gender,
                d.Active
            FROM MDoctors d
            LEFT JOIN MSpecility s ON d.SPId = s.SPId
            WHERE d.DId = @DoctorId";

        return await _secondaryConnection.QueryFirstOrDefaultAsync<DoctorProfileDto>(query, new { DoctorId = doctorId });
    }

    #endregion

    #region Helper Methods

    public async Task<List<ProviderSchedule>> GetProviderSchedulesAsync(long providerId, DateTime fromDate, DateTime toDate)
    {
        var query = @"
            SELECT * FROM ProviderSchedules
            WHERE Active = 1 
            AND ProviderId = @ProviderId
            AND Days > 0
            ORDER BY Priority";

        var schedules = await _primaryConnection.QueryAsync<ProviderSchedule>(query, new { ProviderId = providerId });
        return schedules.ToList();
    }

    public async Task<List<HolidaySchedule>> GetHolidaysAsync(int year, string monthDay, long? siteId)
    {
        var query = @"
            SELECT * FROM HolidaySchedules
            WHERE Years = @Year 
            AND MonthDay = @MonthDay
            AND IsActive = 1
            AND (SiteID = @SiteId OR SiteID = -1)";

        var holidays = await _primaryConnection.QueryAsync<HolidaySchedule>(query, 
            new { Year = year.ToString(), MonthDay = monthDay, SiteId = siteId });
        
        return holidays.ToList();
    }

    public async Task<List<SchBlockTimeslot>> GetBlockedTimeslotsAsync(long providerId, string date, long? siteId)
    {
        var query = @"
            SELECT * FROM SchBlockTimeslots
            WHERE ProviderId = @ProviderId
            AND EffectiveDateTime LIKE @Date + '%'
            AND (SiteId IS NULL OR SiteId = @SiteId)";

        var blocked = await _primaryConnection.QueryAsync<SchBlockTimeslot>(query,
            new { ProviderId = providerId, Date = date, SiteId = siteId });
        
        return blocked.ToList();
    }

    public async Task<List<SchAppointment>> GetExistingAppointmentsAsync(long providerId, string date, long siteId)
    {
        var query = @"
            SELECT * FROM SchAppointments
            WHERE ProviderId = @ProviderId
            AND AppDate = @Date
            AND SiteId = @SiteId
            AND IsActive = 1
            AND AppStatusId = 1";

        var appointments = await _primaryConnection.QueryAsync<SchAppointment>(query,
            new { ProviderId = providerId, Date = date, SiteId = siteId });
        
        return appointments.ToList();
    }

    #endregion
}
