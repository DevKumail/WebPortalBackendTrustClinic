using Asp.Versioning;
using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/appointments/scheduling")]
public class AppointmentsSchedulingController : ControllerBase
{
    private readonly IAppointmentSchedulingService _service;
    private readonly ILogger<AppointmentsSchedulingController> _logger;

    public AppointmentsSchedulingController(IAppointmentSchedulingService service, ILogger<AppointmentsSchedulingController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("availability")]
    public async Task<IActionResult> Availability(
        [FromQuery] long providerId,
        [FromQuery] int locationTypeId,
        [FromQuery] string appDateTime,
        [FromQuery] int durationMinutes,
        [FromQuery] bool allowOverBooking,
        [FromQuery] bool isUMC = false)
    {
        try
        {
            var result = await _service.CheckAvailabilityAsync(providerId, locationTypeId, appDateTime, durationMinutes, allowOverBooking, isUMC);
            if (!result.IsAllowed)
                return Conflict(result);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid availability request");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Availability check failed");
            return StatusCode(500, new { message = "An error occurred while checking availability" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Book([FromBody] Coherent.Core.DTOs.BookAppointmentRequestV2 request)
    {
        try
        {
            var id = await _service.BookAppointmentAsync(request);
            return StatusCode(201, new { appointmentId = id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Book appointment failed");
            return StatusCode(500, new { message = "An error occurred while booking appointment" });
        }
    }

    [HttpPut("{appId:long}")]
    public async Task<IActionResult> Update(long appId, [FromBody] Coherent.Core.DTOs.UpdateAppointmentRequestV2 request)
    {
        try
        {
            var ok = await _service.UpdateAppointmentAsync(appId, request);
            if (!ok)
                return NotFound(new { message = "Appointment not found" });

            return Ok(new { success = true });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update appointment failed");
            return StatusCode(500, new { message = "An error occurred while updating appointment" });
        }
    }

    [HttpPost("{appId:long}/reschedule")]
    public async Task<IActionResult> Reschedule(long appId, [FromBody] Coherent.Core.DTOs.RescheduleAppointmentRequestV2 request)
    {
        try
        {
            var result = await _service.RescheduleAppointmentAsync(appId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reschedule appointment failed");
            return StatusCode(500, new { message = "An error occurred while rescheduling appointment" });
        }
    }

    [HttpPost("{appId:long}/cancel")]
    public async Task<IActionResult> Cancel(long appId, [FromBody] Coherent.Core.DTOs.CancelAppointmentRequestV2 request)
    {
        try
        {
            var result = await _service.CancelAppointmentAsync(appId, request);
            if (!result.Success)
                return NotFound(new { message = "Appointment not found" });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cancel appointment failed");
            return StatusCode(500, new { message = "An error occurred while cancelling appointment" });
        }
    }
}
