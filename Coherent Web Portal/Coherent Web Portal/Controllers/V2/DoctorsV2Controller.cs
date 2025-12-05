using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Coherent.Web.Portal.Controllers.V2;

/// <summary>
/// Doctors API Controller - Version 2 (Mobile App)
/// Requires Security Key authentication for mobile app backend
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
[ThirdPartyAuth] // Requires Security Key authentication
public class DoctorsController : ControllerBase
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly ILogger<DoctorsController> _logger;

    public DoctorsController(
        IDoctorRepository doctorRepository,
        ILogger<DoctorsController> logger)
    {
        _doctorRepository = doctorRepository;
        _logger = logger;
    }

    /// <summary>
    /// 4.2 Get All Doctors (V2 - Mobile App)
    /// </summary>
    [HttpGet("GetAllDoctors")]
    [ProducesResponseType(typeof(List<DoctorProfileDto>), 200)]
    public async Task<IActionResult> GetAllDoctors()
    {
        try
        {
            _logger.LogInformation("V2 - Getting all doctors");

            var doctors = await _doctorRepository.GetAllDoctorsAsync();

            return Ok(doctors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2 - Error getting all doctors");
            return StatusCode(500, new { message = "An error occurred while retrieving doctors" });
        }
    }

    /// <summary>
    /// Get Doctor by ID (V2 - Mobile App)
    /// </summary>
    [HttpGet("{doctorId}")]
    [ProducesResponseType(typeof(DoctorProfileDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDoctorById(int doctorId)
    {
        try
        {
            _logger.LogInformation("V2 - Getting doctor by ID: {DoctorId}", doctorId);

            var doctor = await _doctorRepository.GetDoctorByIdAsync(doctorId);

            if (doctor == null)
            {
                _logger.LogWarning("V2 - Doctor not found - DoctorId: {DoctorId}", doctorId);
                return NotFound(new { message = $"Doctor with ID {doctorId} not found" });
            }

            return Ok(doctor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2 - Error getting doctor by ID: {DoctorId}", doctorId);
            return StatusCode(500, new { message = "An error occurred while retrieving doctor" });
        }
    }
}
