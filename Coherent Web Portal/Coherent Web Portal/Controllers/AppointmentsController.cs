using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

/// <summary>
/// Appointments API Controller
/// Handles appointment booking, modification, and slot availability
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // No token required for mobile app
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
    /// 4.1.1 Get All Appointments by MRNO
    /// </summary>
    [HttpGet("GetAllAppointmentByMRNO")]
    [ProducesResponseType(typeof(List<AppointmentDto>), 200)]
    public async Task<IActionResult> GetAllAppointmentByMRNO([FromQuery] string MRNO)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(MRNO))
                return BadRequest(new { message = "MRNO is required" });

            _logger.LogInformation("Getting appointments for MRNO: {MRNO}", MRNO);

            var appointments = await _appointmentRepository.GetAllAppointmentsByMRNOAsync(MRNO);

            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointments for MRNO: {MRNO}", MRNO);
            return StatusCode(500, new { message = "An error occurred while retrieving appointments" });
        }
    }

    /// <summary>
    /// 4.1.2 Get Available Doctor Slots
    /// </summary>
    [HttpGet("GetAvailableSlotOfDoctor")]
    [ProducesResponseType(typeof(List<DoctorSlotsDto>), 200)]
    public async Task<IActionResult> GetAvailableSlotOfDoctor(
        [FromQuery] long? doctorId,
        [FromQuery] string? prsnlAlias,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        try
        {
            _logger.LogInformation(
                "Getting available slots - DoctorId: {DoctorId}, Alias: {Alias}, From: {From}, To: {To}",
                doctorId, prsnlAlias, fromDate, toDate);

            var request = new GetAvailableSlotsRequest
            {
                DoctorId = doctorId,
                PrsnlAlias = prsnlAlias,
                FromDate = fromDate ?? DateTime.Today,
                ToDate = toDate ?? DateTime.Today.AddDays(7)
            };

            if (!request.DoctorId.HasValue && string.IsNullOrWhiteSpace(request.PrsnlAlias))
            {
                return BadRequest(new { message = "Either DoctorId or PrsnlAlias is required" });
            }

            var slots = await _appointmentRepository.GetAvailableSlotsOfDoctorAsync(request);

            return Ok(slots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available slots");
            return StatusCode(500, new { message = "An error occurred while retrieving available slots" });
        }
    }

    /// <summary>
    /// 4.1.3 Book Appointment
    /// </summary>
    [HttpPost("BookAppointment")]
    [ProducesResponseType(typeof(object), 201)]
    public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Booking appointment - MRNO: {MRNO}, DoctorId: {DoctorId}, DateTime: {DateTime}",
                request.MRNO, request.DoctorId, request.AppointmentDateTime);

            if (string.IsNullOrWhiteSpace(request.MRNO))
                return BadRequest(new { message = "MRNO is required" });

            if (request.DoctorId <= 0)
                return BadRequest(new { message = "Valid DoctorId is required" });

            if (request.AppointmentDateTime < DateTime.Now)
                return BadRequest(new { message = "Appointment date/time cannot be in the past" });

            var appointmentId = await _appointmentRepository.BookAppointmentAsync(request);

            if (appointmentId > 0)
            {
                _logger.LogInformation("Appointment booked successfully - AppId: {AppId}", appointmentId);
                return StatusCode(201, new
                {
                    message = "Appointment booked successfully",
                    appointmentId = appointmentId,
                    status = "scheduled"
                });
            }

            return StatusCode(500, new { message = "Failed to book appointment" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking appointment");
            return StatusCode(500, new { message = "An error occurred while booking the appointment" });
        }
    }

    /// <summary>
    /// 4.1.4 Modify Appointment (Reschedule or Cancel)
    /// </summary>
    [HttpPost("ChangeBookedAppointment")]
    [HttpPut("ChangeBookedAppointment")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> ChangeBookedAppointment([FromBody] ModifyAppointmentRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Modifying appointment - AppId: {AppId}, Status: {Status}",
                request.AppId, request.Status);

            if (request.AppId <= 0)
                return BadRequest(new { message = "Valid AppId is required" });

            if (string.IsNullOrWhiteSpace(request.Status))
                return BadRequest(new { message = "Status is required (rescheduled or cancel)" });

            // Validate appointment exists
            var existingAppointment = await _appointmentRepository.GetAppointmentByIdAsync(request.AppId);
            if (existingAppointment == null)
                return NotFound(new { message = "Appointment not found" });

            // If rescheduling, validate new date/time
            if (request.Status.ToLower() == "rescheduled")
            {
                if (!request.AppointmentDateTime.HasValue)
                    return BadRequest(new { message = "New appointment date/time is required for rescheduling" });

                if (request.AppointmentDateTime.Value < DateTime.Now)
                    return BadRequest(new { message = "New appointment date/time cannot be in the past" });
            }

            var success = await _appointmentRepository.ModifyAppointmentAsync(request);

            if (success)
            {
                _logger.LogInformation("Appointment modified successfully - AppId: {AppId}", request.AppId);
                return Ok(new
                {
                    message = $"Appointment {request.Status} successfully",
                    appointmentId = request.AppId,
                    status = request.Status
                });
            }

            return StatusCode(500, new { message = "Failed to modify appointment" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error modifying appointment");
            return StatusCode(500, new { message = "An error occurred while modifying the appointment" });
        }
    }
}
