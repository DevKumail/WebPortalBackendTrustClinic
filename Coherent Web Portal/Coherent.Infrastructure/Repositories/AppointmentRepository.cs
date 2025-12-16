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
                NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(he.FName, ''), ' ', ISNULL(he.MName, ''), ' ', ISNULL(he.LName, '')))), '') AS DoctorName,
                he.ProvNPI AS DoctorLicenseNo,
                sa.SiteId,
                sa.AppDate       AS AppDateRaw,
                sa.AppDateTime  AS AppDateTimeRaw,
                sa.Duration,
                CASE sa.AppStatusId
                    WHEN 1 THEN 'Scheduled'
                    WHEN 2 THEN 'Rescheduled'
                    WHEN 3 THEN 'Cancelled'
                    ELSE 'Unknown'
                END AS Status,
                sa.EntryDateTime AS CreatedDateRaw,
                sa.EnteredBy    AS CreatedBy
            FROM SchAppointment sa
            LEFT JOIN HREmployee he ON sa.ProviderId = he.EmpId
            WHERE sa.MRNo = @MRNo 
            AND sa.IsActive = 1
            ORDER BY sa.AppDateTime DESC";

        var appointments = await _primaryConnection.QueryAsync<AppointmentDto>(query, new { MRNo = mrNo });
        
        // Convert date strings to DateTime
        foreach (var apt in appointments)
        {
            if (!string.IsNullOrEmpty(apt.AppDateRaw))
            {
                var date = DateStringConversion.StringToDate(apt.AppDateRaw);
                apt.AppointmentDate = date == DateTime.MinValue ? null : date;
            }

            if (!string.IsNullOrEmpty(apt.AppDateTimeRaw))
            {
                var dateTime = DateStringConversion.StringToDate(apt.AppDateTimeRaw);
                apt.AppointmentDateTime = dateTime == DateTime.MinValue ? null : dateTime;
            }

            if (!string.IsNullOrEmpty(apt.CreatedDateRaw))
            {
                var created = DateStringConversion.StringToDate(apt.CreatedDateRaw);
                apt.CreatedDate = created == DateTime.MinValue ? null : created;
            }
        }

        return appointments.ToList();
    }

    #endregion

    #region Book Appointment

    public async Task<long> BookAppointmentAsync(BookAppointmentRequest request)
    {
        try
        {
            var parameters = new DynamicParameters();

            parameters.Add("@bookingID", dbType: DbType.String, direction: ParameterDirection.Output, size: 20);
            parameters.Add("@doctorID", request.DoctorID, DbType.String);
            parameters.Add("@facilityID", request.FacilityID, DbType.String);
            parameters.Add("@serviceID", request.ServiceID, DbType.String);

            parameters.Add("@time", request.Time, DbType.String);
            parameters.Add("@MRNo", request.MRNo, DbType.String);

            await _primaryConnection.ExecuteAsync(
                "MobileAppBookAppointment",
                parameters,
                commandType: CommandType.StoredProcedure);

            var bookingIdString = parameters.Get<string>("@bookingID");

            if (string.IsNullOrWhiteSpace(bookingIdString))
                return 0;

            if (!long.TryParse(bookingIdString, out var bookingId))
                return 0;

            if (bookingId <= 0)
                return 0;

            return bookingId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] BookAppointmentAsync failed: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Modify Appointment

    public async Task<bool> ModifyAppointmentAsync(ModifyAppointmentRequest request)
    {
        try
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var status = request.Status?.ToLowerInvariant();

            if (status == "cancel" || status == "cancelled")
            {
                var parameters = new DynamicParameters();
                parameters.Add("@bookingID", request.AppId.ToString(), DbType.String);

                var rows = await _primaryConnection.ExecuteAsync(
                    "MobileAppCancelAppointment",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return rows > 0;
            }

            if (status == "rescheduled")
            {
                // 1) Cancel old appointment via SP
                var cancelParams = new DynamicParameters();
                cancelParams.Add("@bookingID", request.AppId.ToString(), DbType.String);

                var cancelRows = await _primaryConnection.ExecuteAsync(
                    "MobileAppCancelAppointment",
                    cancelParams,
                    commandType: CommandType.StoredProcedure);

                if (cancelRows <= 0)
                    return false;

                // 2) Book new appointment via SP
                var newRequest = new BookAppointmentRequest
                {
                    DoctorID = request.DoctorID,
                    FacilityID = request.FacilityID,
                    ServiceID = request.ServiceID,
                    Time = request.Time,
                    MRNo = request.MRNo
                };

                var newAppId = await BookAppointmentAsync(newRequest);

                if (newAppId <= 0)
                    return false;

                // Update request to reflect new appointment id for controller responses
                request.AppId = newAppId;

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] ModifyAppointmentAsync failed: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Get Appointment By Id

    public async Task<SchAppointment?> GetAppointmentByIdAsync(long appId)
    {
        try
        {
            var query = "SELECT * FROM SchAppointment WHERE AppId = @AppId";
            return await _primaryConnection.QueryFirstOrDefaultAsync<SchAppointment>(query, new { AppId = appId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetAppointmentByIdAsync failed: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Get Available Slots

    public async Task<List<DoctorSlotsDto>> GetAvailableSlotsOfDoctorAsync(GetAvailableSlotsRequest request)
    {
        var slots = new List<DoctorSlotsDto>();
        long slotId = 1000000; // Starting slot ID

        Console.WriteLine($"[DEBUG] Request - Alias: {request.PrsnlAlias}, From: {request.FromDate}, To: {request.ToDate}");

        // Get provider info - Try with more flexible criteria
        var providerQuery = @"
            SELECT TOP 1 * FROM HREmployee 
            WHERE (
                (@Alias IS NOT NULL AND ProvNPI = @Alias)
            ) 
            AND Active = 1";
        
        Console.WriteLine($"[DEBUG] Searching provider with  Alias={request.PrsnlAlias}");
        
        var provider = await _primaryConnection.QueryFirstOrDefaultAsync<HREmployee>(
            providerQuery,
            new { Alias = request.PrsnlAlias });

        if (provider == null)
        {
            Console.WriteLine($"[DEBUG] Provider not found! Check HREmployee table.");
            return slots;
        }

        Console.WriteLine($"[DEBUG] Provider found - EmpId: {provider.EmpId}, Name: {provider.FName} {provider.LName}");

        // Get provider schedules
        var schedules = await GetProviderSchedulesAsync(provider.EmpId, request.FromDate, request.ToDate);
        Console.WriteLine($"[DEBUG] Found {schedules.Count} schedules");

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

            Console.WriteLine($"[DEBUG] Processing date: {currentDate:yyyy-MM-dd} ({currentDate.DayOfWeek})");

            // Filter schedules valid for current date
            var validSchedules = schedules.Where(ps =>
            {
                var startDateValid = string.IsNullOrEmpty(ps.StartDate) || DateStringConversion.StringToDate(ps.StartDate).Date <= currentDate.Date;
                var endDateValid = string.IsNullOrEmpty(ps.EndDate) || DateStringConversion.StringToDate(ps.EndDate).Date >= currentDate.Date;
                var dayValid = DateStringConversion.DayExists(currentDate.DayOfWeek.ToString(), ps.Days);
                
                Console.WriteLine($"[DEBUG] Schedule {ps.PSId}: StartDate={ps.StartDate} (valid:{startDateValid}), EndDate={ps.EndDate} (valid:{endDateValid}), Days={ps.Days} (valid:{dayValid})");
                
                return startDateValid && endDateValid && dayValid;
            }).OrderBy(ps => ps.Priority).ToList();

            Console.WriteLine($"[DEBUG] Valid schedules for {currentDate:yyyy-MM-dd}: {validSchedules.Count}");

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
        try
        {
            var query = @"
                SELECT * FROM ProviderSchedule
                WHERE Active = 1 
                AND ProviderId = @ProviderId
                AND Days > 0
                ORDER BY Priority";

            var schedules = await _primaryConnection.QueryAsync<ProviderSchedule>(query, new { ProviderId = providerId });
            return schedules.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetProviderSchedulesAsync failed: {ex.Message}");
            return new List<ProviderSchedule>();
        }
    }

    public async Task<List<HolidaySchedule>> GetHolidaysAsync(int year, string monthDay, long? siteId)
    {
        try
        {
            var query = @"
                SELECT * FROM HolidaySchedule
                WHERE Years = @Year 
                AND MonthDay = @MonthDay
                AND IsActive = 1
                AND (SiteID = @SiteId OR SiteID = -1)";

            var holidays = await _primaryConnection.QueryAsync<HolidaySchedule>(query, 
                new { Year = year.ToString(), MonthDay = monthDay, SiteId = siteId });
            
            return holidays.ToList();
        }
        catch (Exception)
        {
            // HolidaySchedules table doesn't exist or query failed, return empty list
            Console.WriteLine("[DEBUG] HolidaySchedules table not found or query failed - skipping holiday check");
            return new List<HolidaySchedule>();
        }
    }

    public async Task<List<SchBlockTimeslot>> GetBlockedTimeslotsAsync(long providerId, string date, long? siteId)
    {
        try
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
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetBlockedTimeslotsAsync failed: {ex.Message}");
            return new List<SchBlockTimeslot>();
        }
    }

    public async Task<List<SchAppointment>> GetExistingAppointmentsAsync(long providerId, string date, long siteId)
    {
        try
        {
            var query = @"
                SELECT * FROM SchAppointment
                WHERE ProviderId = @ProviderId
                AND AppDate = @Date
                AND SiteId = @SiteId
                AND IsActive = 1
                AND AppStatusId = 1";

            var appointments = await _primaryConnection.QueryAsync<SchAppointment>(query,
                new { ProviderId = providerId, Date = date, SiteId = siteId });
            
            return appointments.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] GetExistingAppointmentsAsync failed: {ex.Message}");
            return new List<SchAppointment>();
        }
    }

    #endregion
}
