using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Coherent.Web.Portal.Controllers.V2;

/// <summary>
/// Patients API Controller - Version 2 (Mobile App)
/// Endpoints for mobile app patient operations
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
public class PatientsController : ControllerBase
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        IPatientRepository patientRepository,
        ILogger<PatientsController> logger)
    {
        _patientRepository = patientRepository;
        _logger = logger;
    }

    /// <summary>
    /// Set patient as mobile user when they register on mobile app
    /// </summary>
    /// <param name="mrNo">Medical Record Number</param>
    /// <returns>Success or failure status</returns>
    [HttpPatch("{mrNo}/mobile-user")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> SetMobileUser(string mrNo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(mrNo))
                return BadRequest(new { message = "MRNo is required" });

            _logger.LogInformation("V2 - Setting IsMobileUser=true for MRNo: {MRNo}", mrNo);

            // Check if patient exists
            var patient = await _patientRepository.GetPatientByMRNoAsync(mrNo);
            if (patient == null)
            {
                _logger.LogWarning("V2 - Patient not found - MRNo: {MRNo}", mrNo);
                return NotFound(new { message = $"Patient with MRNo {mrNo} not found" });
            }

            // Update IsMobileUser to true
            var success = await _patientRepository.UpdateIsMobileUserAsync(mrNo, true);

            if (success)
            {
                _logger.LogInformation("V2 - Successfully set IsMobileUser=true for MRNo: {MRNo}", mrNo);
                return Ok(new { message = "Patient marked as mobile user successfully", mrNo = mrNo, isMobileUser = true });
            }
            else
            {
                _logger.LogWarning("V2 - Failed to update IsMobileUser for MRNo: {MRNo}", mrNo);
                return StatusCode(500, new { message = "Failed to update patient mobile user status" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2 - Error setting mobile user for MRNo: {MRNo}", mrNo);
            return StatusCode(500, new { message = "An error occurred while updating patient" });
        }
    }
}
