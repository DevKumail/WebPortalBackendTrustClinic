# Doctor Appointment Slots - Complete Code Snippet

## Overview
This module handles doctor appointment slot management including:
- Get available slots for a doctor
- Book appointment
- Modify/Cancel appointment
- Get all appointments by patient MRNO

---

## 1. DTOs (Data Transfer Objects)

### AppointmentDTOs.cs

```csharp
using System.Text.Json.Serialization;

namespace Coherent.Core.DTOs;

/// <summary>
/// Doctor available slot DTO
/// </summary>
public class DoctorAvailableSlotDto
{
    public string? SlotId { get; set; }
    public string? DttmFrom { get; set; }      // Format: yyyy-MM-dd HH:mm:ss
    public string? DttmTo { get; set; }        // Format: yyyy-MM-dd HH:mm:ss
    public string? DttmDuration { get; set; }  // Duration in minutes
    public string? SlotState { get; set; }     // ACTIVE, BOOKED, BLOCKED
    public string? SlotType { get; set; }      // F=Free, B=Booked
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
    public string? DoctorLicenseNo { get; set; }
    public string? Speciality { get; set; }
    public long SiteId { get; set; }
    public string? SiteName { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public DateTime? AppointmentDateTime { get; set; }
    public int Duration { get; set; }
    public string? Status { get; set; }  // Scheduled, Rescheduled, Cancelled
    public DateTime? CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Book appointment request
/// </summary>
public class BookAppointmentRequest
{
    [JsonPropertyName("doctorID")]
    public string? DoctorID { get; set; }

    [JsonPropertyName("facilityID")]
    public string? FacilityID { get; set; }

    [JsonPropertyName("serviceID")]
    public string? ServiceID { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }  // Format: yyyyMMddHHmmss

    [JsonPropertyName("mrNo")]
    public string? MRNo { get; set; }
}

/// <summary>
/// Cancel appointment request
/// </summary>
public class CancelAppointmentRequest
{
    [JsonPropertyName("appBookingId")]
    public long AppBookingId { get; set; }
}

/// <summary>
/// Modify appointment request
/// </summary>
public class ModifyAppointmentRequest
{
    [JsonPropertyName("appId")]
    public long AppId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }  // rescheduled, cancel

    [JsonPropertyName("doctorID")]
    public string? DoctorID { get; set; }

    [JsonPropertyName("facilityID")]
    public string? FacilityID { get; set; }

    [JsonPropertyName("serviceID")]
    public string? ServiceID { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("mrNo")]
    public string? MRNo { get; set; }
}

/// <summary>
/// Get available slots request
/// </summary>
public class GetAvailableSlotsRequest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? PrsnlAlias { get; set; }  // Doctor NPI/License No
}
```

---

## 2. Entity Models

### ProviderSchedule.cs

```csharp
namespace Coherent.Domain.Entities;

/// <summary>
/// Provider schedule entity - Used for managing doctor appointment schedules
/// </summary>
public class ProviderSchedule
{
    public long PSId { get; set; }
    public long ProviderId { get; set; }
    public long SiteId { get; set; }
    public long? UsageId { get; set; }
    public string? StartTime { get; set; }       // Format: "08:00 AM"
    public string? EndTime { get; set; }         // Format: "05:00 PM"
    public long Days { get; set; }               // Bit flags: Mon=1, Tue=2, Wed=4, Thu=8, Fri=16, Sat=32, Sun=64
    public string? StartDate { get; set; }       // Format: YYYYMMDDHHMMSS
    public string? EndDate { get; set; }         // Format: YYYYMMDDHHMMSS
    public string? BreakStartTime { get; set; }
    public string? BreakEndTime { get; set; }
    public string? BreakReason { get; set; }
    public int? AppPerHour { get; set; }
    public int? MaxOverloadApps { get; set; }
    public int Priority { get; set; }
    public bool Active { get; set; }
}
```

### HREmployee.cs

```csharp
namespace Coherent.Domain.Entities;

public class HREmployee
{
    public long EmpId { get; set; }
    public string? Prefix { get; set; }
    public string? FName { get; set; }
    public string? MName { get; set; }
    public string? LName { get; set; }
    public string? ProvNPI { get; set; }      // Doctor License/NPI Number
    public string? Speciality { get; set; }
    public bool Active { get; set; }
}
```

### SchAppointment.cs

```csharp
namespace Coherent.Domain.Entities;

public class SchAppointment
{
    public long AppId { get; set; }
    public string? MRNo { get; set; }
    public long ProviderId { get; set; }
    public long SiteId { get; set; }
    public string? AppDate { get; set; }       // Format: YYYYMMDD
    public string? AppDateTime { get; set; }   // Format: YYYYMMDDHHMMSS
    public int Duration { get; set; }          // In minutes
    public int AppStatusId { get; set; }       // 1=Scheduled, 2=Rescheduled, 3=Cancelled
    public bool IsActive { get; set; }
}
```

---

## 3. Repository Interface

### IAppointmentRepository.cs

```csharp
using Coherent.Core.DTOs;
using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

public interface IAppointmentRepository
{
    // Appointment Management
    Task<List<AppointmentDto>> GetAllAppointmentsByMRNOAsync(string mrNo);
    Task<long> BookAppointmentAsync(BookAppointmentRequest request);
    Task<bool> ModifyAppointmentAsync(ModifyAppointmentRequest request);
    Task<SchAppointment?> GetAppointmentByIdAsync(long appId);
    
    // Available Slots
    Task<List<DoctorSlotsDto>> GetAvailableSlotsOfDoctorAsync(GetAvailableSlotsRequest request);
    
    // Helper methods for slot calculation
    Task<List<ProviderSchedule>> GetProviderSchedulesAsync(long providerId, DateTime fromDate, DateTime toDate);
    Task<List<HolidaySchedule>> GetHolidaysAsync(int year, string monthDay, long? siteId);
    Task<List<SchBlockTimeslot>> GetBlockedTimeslotsAsync(long providerId, string date, long? siteId);
    Task<List<SchAppointment>> GetExistingAppointmentsAsync(long providerId, string date, long siteId);
}
```

---

## 4. Repository Implementation

### AppointmentRepository.cs

```csharp
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Coherent.Infrastructure.Helpers;
using Dapper;
using System.Data;
using System.Globalization;

namespace Coherent.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly IDbConnection _primaryConnection;
    private readonly IDbConnection _secondaryConnection;

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
                CONCAT(he.FName, ' ', he.LName) AS DoctorName,
                he.ProvNPI AS DoctorLicenseNo,
                sa.SiteId,
                sa.AppDate AS AppDateRaw,
                sa.AppDateTime AS AppDateTimeRaw,
                sa.Duration,
                CASE sa.AppStatusId
                    WHEN 1 THEN 'Scheduled'
                    WHEN 2 THEN 'Rescheduled'
                    WHEN 3 THEN 'Cancelled'
                    ELSE 'Unknown'
                END AS Status,
                sa.EntryDateTime AS CreatedDateRaw,
                sa.EnteredBy AS CreatedBy
            FROM SchAppointment sa
            LEFT JOIN HREmployee he ON sa.ProviderId = he.EmpId
            WHERE sa.MRNo = @MRNo 
            AND sa.IsActive = 1
            ORDER BY sa.AppDateTime DESC";

        var appointments = await _primaryConnection.QueryAsync<AppointmentDto>(query, new { MRNo = mrNo });
        
        foreach (var apt in appointments)
        {
            if (!string.IsNullOrEmpty(apt.AppDateRaw))
                apt.AppointmentDate = DateStringConversion.StringToDate(apt.AppDateRaw);

            if (!string.IsNullOrEmpty(apt.AppDateTimeRaw))
                apt.AppointmentDateTime = DateStringConversion.StringToDate(apt.AppDateTimeRaw);
        }

        return appointments.ToList();
    }

    #endregion

    #region Book Appointment

    public async Task<long> BookAppointmentAsync(BookAppointmentRequest request)
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
        return long.TryParse(bookingIdString, out var bookingId) ? bookingId : 0;
    }

    #endregion

    #region Modify Appointment

    public async Task<bool> ModifyAppointmentAsync(ModifyAppointmentRequest request)
    {
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
            // Cancel old appointment
            var cancelParams = new DynamicParameters();
            cancelParams.Add("@bookingID", request.AppId.ToString(), DbType.String);
            await _primaryConnection.ExecuteAsync("MobileAppCancelAppointment", cancelParams, commandType: CommandType.StoredProcedure);

            // Book new appointment
            var newRequest = new BookAppointmentRequest
            {
                DoctorID = request.DoctorID,
                FacilityID = request.FacilityID,
                ServiceID = request.ServiceID,
                Time = request.Time,
                MRNo = request.MRNo
            };

            var newAppId = await BookAppointmentAsync(newRequest);
            return newAppId > 0;
        }

        return false;
    }

    #endregion

    #region Get Appointment By Id

    public async Task<SchAppointment?> GetAppointmentByIdAsync(long appId)
    {
        var query = "SELECT * FROM SchAppointment WHERE AppId = @AppId";
        return await _primaryConnection.QueryFirstOrDefaultAsync<SchAppointment>(query, new { AppId = appId });
    }

    #endregion

    #region Get Available Slots - MAIN LOGIC

    public async Task<List<DoctorSlotsDto>> GetAvailableSlotsOfDoctorAsync(GetAvailableSlotsRequest request)
    {
        var slots = new List<DoctorSlotsDto>();
        long slotId = 1000000;

        // Step 1: Get provider info from HREmployee table
        var providerQuery = @"
            SELECT TOP 1 * FROM HREmployee 
            WHERE ProvNPI = @Alias AND Active = 1";
        
        var provider = await _primaryConnection.QueryFirstOrDefaultAsync<HREmployee>(
            providerQuery, new { Alias = request.PrsnlAlias });

        if (provider == null)
            return slots;

        // Step 2: Get provider schedules
        var schedules = await GetProviderSchedulesAsync(provider.EmpId, request.FromDate, request.ToDate);

        DateTime currentDate = request.FromDate;

        // Step 3: Loop through each date in range
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

            // Step 4: Filter schedules valid for current date
            var validSchedules = schedules.Where(ps =>
            {
                var startDateValid = string.IsNullOrEmpty(ps.StartDate) || 
                    DateStringConversion.StringToDate(ps.StartDate).Date <= currentDate.Date;
                var endDateValid = string.IsNullOrEmpty(ps.EndDate) || 
                    DateStringConversion.StringToDate(ps.EndDate).Date >= currentDate.Date;
                var dayValid = DateStringConversion.DayExists(currentDate.DayOfWeek.ToString(), ps.Days);
                
                return startDateValid && endDateValid && dayValid;
            }).OrderBy(ps => ps.Priority).ToList();

            foreach (var schedule in validSchedules)
            {
                try
                {
                    // Step 5: Parse start and end times
                    var startTime = DateTime.ParseExact(schedule.StartTime ?? "08:00 AM", "hh:mm tt", CultureInfo.InvariantCulture);
                    var endTime = DateTime.ParseExact(schedule.EndTime ?? "05:00 PM", "hh:mm tt", CultureInfo.InvariantCulture);
                    
                    DateTime timeSlotStart = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 
                        startTime.Hour, startTime.Minute, 0);
                    DateTime timeSlotEnd = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 
                        endTime.Hour, endTime.Minute, 0);

                    // Step 6: Parse break times if any
                    DateTime? breakStart = null;
                    DateTime? breakEnd = null;
                    if (!string.IsNullOrEmpty(schedule.BreakStartTime))
                        breakStart = DateStringConversion.TimeStringToDate(currentDate, schedule.BreakStartTime);
                    if (!string.IsNullOrEmpty(schedule.BreakEndTime))
                        breakEnd = DateStringConversion.TimeStringToDate(currentDate, schedule.BreakEndTime);

                    // Step 7: Generate 15-minute slots
                    var availableSlots = new List<DateTime>();
                    while (timeSlotStart < timeSlotEnd)
                    {
                        // Skip slots during break time
                        if (breakStart == null || timeSlotStart < breakStart.Value || timeSlotStart >= breakEnd.Value)
                        {
                            availableSlots.Add(timeSlotStart);
                        }
                        timeSlotStart = timeSlotStart.AddMinutes(15);
                    }

                    // Step 8: Remove holidays
                    var holidays = await GetHolidaysAsync(currentDate.Year, monthDay, schedule.SiteId);
                    foreach (var holiday in holidays)
                    {
                        if (string.IsNullOrEmpty(holiday.StartingTime) && string.IsNullOrEmpty(holiday.EndingTime))
                        {
                            availableSlots.RemoveAll(s => s.Date == currentDate.Date);
                        }
                    }

                    // Step 9: Remove blocked times
                    var blockedTimes = await GetBlockedTimeslotsAsync(provider.EmpId, shortDate, schedule.SiteId);
                    foreach (var blocked in blockedTimes)
                    {
                        var blockedTime = DateStringConversion.StringToDate(blocked.EffectiveDateTime);
                        availableSlots.RemoveAll(s => s == blockedTime);
                    }

                    // Step 10: Remove existing appointments
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

                    // Step 11: Create DTO for available slots
                    if (availableSlots.Any())
                    {
                        var doctorSlot = new DoctorSlotsDto
                        {
                            PrsnlId = provider.EmpId.ToString(),
                            PrsnlName = $"{provider.Prefix} {provider.FName} {provider.MName} {provider.LName}".Trim(),
                            PrsnlAlias = provider.ProvNPI,
                            ResourceCd = provider.EmpId.ToString(),
                            ResourceName = $"{provider.FName} {provider.LName}",
                            SpecialityId = "1",
                            SpecialityName = provider.Speciality,
                            FacilityId = "1",
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
                    Console.WriteLine($"Error processing schedule: {ex.Message}");
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        return slots;
    }

    #endregion

    #region Helper Methods

    public async Task<List<ProviderSchedule>> GetProviderSchedulesAsync(long providerId, DateTime fromDate, DateTime toDate)
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

    public async Task<List<HolidaySchedule>> GetHolidaysAsync(int year, string monthDay, long? siteId)
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

    #endregion
}
```

---

## 5. API Controller

### AppointmentsController.cs

```csharp
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System.Globalization;

namespace Coherent.Web.Portal.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentRepository appointmentRepository,
        ILogger<AppointmentsController> logger)
    {
        _appointmentRepository = appointmentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get All Appointments by Patient MRNO
    /// </summary>
    [HttpGet("GetAllAppointmentByMRNO")]
    public async Task<IActionResult> GetAllAppointmentByMRNO([FromQuery] string MRNO)
    {
        if (string.IsNullOrWhiteSpace(MRNO))
            return BadRequest(new { message = "MRNO is required" });

        var appointments = await _appointmentRepository.GetAllAppointmentsByMRNOAsync(MRNO);
        return Ok(appointments);
    }

    /// <summary>
    /// Get Available Doctor Slots
    /// </summary>
    [AllowAnonymous]
    [HttpGet("GetAvailableSlotOfDoctor")]
    public async Task<IActionResult> GetAvailableSlotOfDoctor(
        [FromQuery] string? prsnlAlias,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var request = new GetAvailableSlotsRequest
        {
            PrsnlAlias = prsnlAlias,
            FromDate = fromDate ?? DateTime.Today,
            ToDate = toDate ?? DateTime.Today.AddDays(7)
        };

        if (string.IsNullOrWhiteSpace(request.PrsnlAlias))
            return BadRequest(new { message = "PrsnlAlias (Doctor License No) is required" });

        var slots = await _appointmentRepository.GetAvailableSlotsOfDoctorAsync(request);

        return Ok(new { data = slots });
    }

    /// <summary>
    /// Book Appointment
    /// </summary>
    [HttpPost("BookAppointment")]
    public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MRNo))
            return BadRequest(new { message = "MRNo is required" });

        if (string.IsNullOrWhiteSpace(request.DoctorID))
            return BadRequest(new { message = "doctorID is required" });

        if (string.IsNullOrWhiteSpace(request.FacilityID))
            return BadRequest(new { message = "facilityID is required" });

        if (string.IsNullOrWhiteSpace(request.Time))
            return BadRequest(new { message = "time is required" });

        if (!DateTime.TryParseExact(request.Time, "yyyyMMddHHmmss", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var appointmentDateTime))
            return BadRequest(new { message = "Invalid time format. Expected yyyyMMddHHmmss" });

        if (appointmentDateTime < DateTime.Now)
            return BadRequest(new { message = "Appointment date/time cannot be in the past" });

        var appointmentId = await _appointmentRepository.BookAppointmentAsync(request);

        if (appointmentId > 0)
        {
            return StatusCode(201, new
            {
                message = "Appointment booked successfully",
                appointmentId = appointmentId,
                status = "scheduled"
            });
        }

        return StatusCode(500, new { message = "Failed to book appointment" });
    }

    /// <summary>
    /// Modify/Reschedule Appointment
    /// </summary>
    [HttpPut("ChangeBookedAppointment")]
    public async Task<IActionResult> ChangeBookedAppointment([FromBody] ModifyAppointmentRequest request)
    {
        if (request.AppId <= 0)
            return BadRequest(new { message = "Valid AppId is required" });

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "Status is required (rescheduled or cancel)" });

        var existingAppointment = await _appointmentRepository.GetAppointmentByIdAsync(request.AppId);
        if (existingAppointment == null)
            return NotFound(new { message = "Appointment not found" });

        var success = await _appointmentRepository.ModifyAppointmentAsync(request);

        if (success)
        {
            return Ok(new
            {
                message = $"Appointment {request.Status} successfully",
                appointmentId = request.AppId,
                status = request.Status
            });
        }

        return StatusCode(500, new { message = "Failed to modify appointment" });
    }

    /// <summary>
    /// Cancel Appointment
    /// </summary>
    [HttpPost("CancelAppointment")]
    public async Task<IActionResult> CancelAppointment([FromBody] CancelAppointmentRequest request)
    {
        if (request.AppBookingId <= 0)
            return BadRequest(new { message = "Valid appBookingId is required" });

        var modifyRequest = new ModifyAppointmentRequest
        {
            AppId = request.AppBookingId,
            Status = "cancel"
        };

        var success = await _appointmentRepository.ModifyAppointmentAsync(modifyRequest);

        if (success)
        {
            return Ok(new
            {
                message = "Appointment cancelled successfully",
                appBookingId = request.AppBookingId,
                status = "cancelled"
            });
        }

        return StatusCode(500, new { message = "Failed to cancel appointment" });
    }
}
```

---

## 6. API Endpoints Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v2/Appointments/GetAvailableSlotOfDoctor` | Get available slots for a doctor |
| GET | `/api/v2/Appointments/GetAllAppointmentByMRNO` | Get all appointments for a patient |
| POST | `/api/v2/Appointments/BookAppointment` | Book a new appointment |
| PUT | `/api/v2/Appointments/ChangeBookedAppointment` | Reschedule/Modify appointment |
| POST | `/api/v2/Appointments/CancelAppointment` | Cancel an appointment |

---

## 7. API Examples

### Get Available Slots

**Request:**
```
GET /api/v2/Appointments/GetAvailableSlotOfDoctor?prsnlAlias=DOC123&fromDate=2026-01-05&toDate=2026-01-12
```

**Response:**
```json
{
  "data": [
    {
      "prsnlId": "101",
      "prsnlName": "Dr. Ahmed Khan",
      "prsnlAlias": "DOC123",
      "specialityName": "Fertility",
      "facilityId": "1",
      "availableSlots": [
        {
          "slotId": "1000000",
          "dttmFrom": "2026-01-06 09:00:00",
          "dttmTo": "2026-01-06 09:15:00",
          "dttmDuration": "15",
          "slotState": "ACTIVE",
          "slotType": "F"
        },
        {
          "slotId": "1000001",
          "dttmFrom": "2026-01-06 09:15:00",
          "dttmTo": "2026-01-06 09:30:00",
          "dttmDuration": "15",
          "slotState": "ACTIVE",
          "slotType": "F"
        }
      ]
    }
  ]
}
```

### Book Appointment

**Request:**
```
POST /api/v2/Appointments/BookAppointment
Content-Type: application/json

{
  "doctorID": "101",
  "facilityID": "1",
  "serviceID": "1",
  "time": "20260106090000",
  "mrNo": "MR00001"
}
```

**Response:**
```json
{
  "message": "Appointment booked successfully",
  "appointmentId": 12345,
  "status": "scheduled"
}
```

### Cancel Appointment

**Request:**
```
POST /api/v2/Appointments/CancelAppointment
Content-Type: application/json

{
  "appBookingId": 12345
}
```

**Response:**
```json
{
  "message": "Appointment cancelled successfully",
  "appBookingId": 12345,
  "status": "cancelled"
}
```

---

## 8. Database Tables Used

| Table | Purpose |
|-------|---------|
| `HREmployee` | Doctor/Provider information |
| `ProviderSchedule` | Doctor working hours and days |
| `HolidaySchedule` | Hospital holidays |
| `SchBlockTimeslots` | Blocked time slots |
| `SchAppointment` | Booked appointments |

---

## 9. Slot Calculation Logic Flow

```
1. Get Doctor Info from HREmployee (using ProvNPI/License No)
      ↓
2. Get Doctor's Schedule from ProviderSchedule
      ↓
3. For Each Day in Date Range:
      ↓
   3.1 Skip weekends
   3.2 Filter valid schedules for that day
   3.3 Generate 15-minute slots from StartTime to EndTime
   3.4 Remove break time slots
   3.5 Remove holiday slots
   3.6 Remove blocked slots
   3.7 Remove already booked appointment slots
      ↓
4. Return Available Slots
```
