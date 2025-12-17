using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers.V2;

/// <summary>
/// Patient Health API Controller - Version 2 (Mobile App)
/// Requires Security Key authentication for mobile app backend
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
[ThirdPartyAuth] // Requires Security Key authentication
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
    /// 4.3 Get Vital Signs by MRNO (V2 - Mobile App)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("GetVitalSignsByMRNO")]
    [ProducesResponseType(typeof(List<VitalSignsDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetVitalSignsByMRNO([FromQuery] string MRNO, [FromQuery] int limit = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(MRNO))
                return BadRequest(new { message = "MRNO is required" });

            _logger.LogInformation("V2 - Getting vital signs for MRNO: {MRNO}", MRNO);

            var vitalSigns = await _patientHealthRepository.GetVitalSignsByMRNOAsync(MRNO, limit);

            if (vitalSigns == null || vitalSigns.Count == 0)
            {
                _logger.LogWarning("V2 - Vital signs not found for MRNO: {MRNO}", MRNO);
                return NotFound(new { message = $"Vital signs not found for MRNO {MRNO}" });
            }

            return Ok(vitalSigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2 - Error getting vital signs for MRNO: {MRNO}", MRNO);
            return StatusCode(500, new { message = "An error occurred while retrieving vital signs" });
        }
    }

    /// <summary>
    /// 4.4 Get Medications by MRNO (V2 - Mobile App)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("GetMedicationsByMRNO")]
    [ProducesResponseType(typeof(List<MedicationDto>), 200)]
    public async Task<IActionResult> GetMedicationsByMRNO([FromQuery] string MRNO)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(MRNO))
                return BadRequest(new { message = "MRNO is required" });

            _logger.LogInformation("V2 - Getting medications for MRNO: {MRNO}", MRNO);

            var medications = await _patientHealthRepository.GetMedicationsByMRNOAsync(MRNO);

            return Ok(medications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2 - Error getting medications for MRNO: {MRNO}", MRNO);
            return StatusCode(500, new { message = "An error occurred while retrieving medications" });
        }
    }
    [AllowAnonymous]
    [HttpGet("GetMedicationsByMRNOV2")]
    [ProducesResponseType(typeof(List<MedicationV2Dto>), 200)]
    public async Task<IActionResult> GetMedicationsByMRNOV2([FromQuery] string MRNO)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(MRNO))
                return BadRequest(new { message = "MRNO is required" });

            _logger.LogInformation("V2 - Getting medications (payload v2) for MRNO: {MRNO}", MRNO);

            var medications = await _patientHealthRepository.GetMedicationsV2ByMRNOAsync(MRNO);

            return Ok(medications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2 - Error getting medications (payload v2) for MRNO: {MRNO}", MRNO);
            return StatusCode(500, new { message = "An error occurred while retrieving medications" });
        }
    }

    /// <summary>
    /// 4.5 Get Allergies by MRNO (V2 - Mobile App)
    /// </summary>
    /// 

    [AllowAnonymous]
    [HttpGet("GetAllergyByMRNO")]
    [ProducesResponseType(typeof(List<AllergyDto>), 200)]
    public async Task<IActionResult> GetAllergyByMRNO([FromQuery] string MRNO)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(MRNO))
                return BadRequest(new { message = "MRNO is required" });

            _logger.LogInformation("V2 - Getting allergies for MRNO: {MRNO}", MRNO);

            var allergies = await _patientHealthRepository.GetAllergiesByMRNOAsync(MRNO);

            return Ok(allergies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2 - Error getting allergies for MRNO: {MRNO}", MRNO);
            return StatusCode(500, new { message = "An error occurred while retrieving allergies" });
        }
    }

    [AllowAnonymous]
    [HttpGet("GetDiagnosisByMRNO")]
    [ProducesResponseType(typeof(List<DiagnosisDto>), 200)]
    public async Task<IActionResult> GetDiagnosisByMRNO([FromQuery] string MRNO)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(MRNO))
                return BadRequest(new { message = "MRNO is required" });

            _logger.LogInformation("V2 - Getting diagnosis for MRNO: {MRNO}", MRNO);

            var diagnosis = await _patientHealthRepository.GetDiagnosisByMRNOAsync(MRNO);

            return Ok(diagnosis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2 - Error getting diagnosis for MRNO: {MRNO}", MRNO);
            return StatusCode(500, new { message = "An error occurred while retrieving diagnosis" });
        }
    }
}
