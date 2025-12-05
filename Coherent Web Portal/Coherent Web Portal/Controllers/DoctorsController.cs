using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

/// <summary>
/// Doctors API Controller
/// Provides doctor profiles and information
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // No token required for mobile app
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
    /// 4.2 Get All Doctors
    /// Returns full doctor profiles from CoherentMobApp database
    /// </summary>
    [HttpGet("GetAllDoctors")]
    [ProducesResponseType(typeof(List<DoctorProfileDto>), 200)]
    public async Task<IActionResult> GetAllDoctors()
    {
        try
        {
            _logger.LogInformation("Getting all doctors");

            var doctors = await _doctorRepository.GetAllDoctorsAsync();

            return Ok(doctors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all doctors");
            return StatusCode(500, new { message = "An error occurred while retrieving doctors" });
        }
    }

    /// <summary>
    /// Get Doctor by ID
    /// Returns full doctor profile by ID
    /// </summary>
    [HttpGet("{doctorId}")]
    [ProducesResponseType(typeof(DoctorProfileDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDoctorById(int doctorId)
    {
        try
        {
            _logger.LogInformation("Getting doctor by ID: {DoctorId}", doctorId);

            var doctor = await _doctorRepository.GetDoctorByIdAsync(doctorId);

            if (doctor == null)
            {
                _logger.LogWarning("Doctor not found - DoctorId: {DoctorId}", doctorId);
                return NotFound(new { message = $"Doctor with ID {doctorId} not found" });
            }

            return Ok(doctor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting doctor by ID: {DoctorId}", doctorId);
            return StatusCode(500, new { message = "An error occurred while retrieving doctor" });
        }
    }
}
