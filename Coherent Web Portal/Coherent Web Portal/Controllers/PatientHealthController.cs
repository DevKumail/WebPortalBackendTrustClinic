using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

/// <summary>
/// Patient Health API Controller
/// Provides vital signs, medications, and allergies information
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // No token required for mobile app
public class PatientHealthController : ControllerBase
{
    private readonly IPatientHealthRepository _patientHealthRepository;
    private readonly ILogger<PatientHealthController> _logger;

    public PatientHealthController(
        IPatientHealthRepository patientHealthRepository,
        ILogger<PatientHealthController> logger)
    {
        _patientHealthRepository = patientHealthRepository;
        _logger = logger;
    }

    /// <summary>
    /// 4.3 Get Vital Signs by MRNO
    /// Returns vital signs including BMI, weight, height, temperature
    /// </summary>
    [HttpGet("GetVitalSignsByMRNO")]
    [ProducesResponseType(typeof(List<VitalSignsDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetVitalSignsByMRNO([FromQuery] string MRNO, [FromQuery] int limit = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(MRNO))
                return BadRequest(new { message = "MRNO is required" });

            _logger.LogInformation("Getting vital signs for MRNO: {MRNO}", MRNO);

            var vitalSigns = await _patientHealthRepository.GetVitalSignsByMRNOAsync(MRNO, limit);

            if (vitalSigns == null || vitalSigns.Count == 0)
            {
                _logger.LogWarning("Vital signs not found for MRNO: {MRNO}", MRNO);
                return NotFound(new { message = $"Vital signs not found for MRNO {MRNO}" });
            }

            return Ok(vitalSigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vital signs for MRNO: {MRNO}", MRNO);
            return StatusCode(500, new { message = "An error occurred while retrieving vital signs" });
        }
    }

    /// <summary>
    /// 4.4 Get Medications by MRNO
    /// Returns medication list prescribed by doctors
    /// </summary>
    [HttpGet("GetMedicationsByMRNO")]
    [ProducesResponseType(typeof(List<MedicationDto>), 200)]
    public async Task<IActionResult> GetMedicationsByMRNO([FromQuery] string MRNO)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(MRNO))
                return BadRequest(new { message = "MRNO is required" });

            _logger.LogInformation("Getting medications for MRNO: {MRNO}", MRNO);

            var medications = await _patientHealthRepository.GetMedicationsByMRNOAsync(MRNO);

            return Ok(medications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting medications for MRNO: {MRNO}", MRNO);
            return StatusCode(500, new { message = "An error occurred while retrieving medications" });
        }
    }

    /// <summary>
    /// 4.5 Get Allergies by MRNO
    /// Returns patient allergy history
    /// </summary>
    [HttpGet("GetAllergyByMRNO")]
    [ProducesResponseType(typeof(List<AllergyDto>), 200)]
    public async Task<IActionResult> GetAllergyByMRNO([FromQuery] string MRNO)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(MRNO))
                return BadRequest(new { message = "MRNO is required" });

            _logger.LogInformation("Getting allergies for MRNO: {MRNO}", MRNO);

            var allergies = await _patientHealthRepository.GetAllergiesByMRNOAsync(MRNO);

            return Ok(allergies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting allergies for MRNO: {MRNO}", MRNO);
            return StatusCode(500, new { message = "An error occurred while retrieving allergies" });
        }
    }
}
