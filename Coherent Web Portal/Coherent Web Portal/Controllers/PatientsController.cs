using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Coherent.Web.Portal.Controllers;

/// <summary>
/// Patients API Controller - Version 1 (Web Portal)
/// Provides endpoints for patient search and retrieval
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[AllowAnonymous] // No authentication required for V1
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
    /// Search patients with pagination
    /// </summary>
    /// <param name="request">Search parameters</param>
    /// <returns>Paginated list of patients</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PaginatedPatientResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchPatients([FromQuery] PatientSearchRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Searching patients - MRNo: {MRNo}, Name: {Name}, Page: {Page}, PageSize: {PageSize}",
                request.MRNo, request.Name, request.PageNumber, request.PageSize);

            // Validate pagination parameters
            if (request.PageNumber < 1)
                request.PageNumber = 1;

            if (request.PageSize < 1 || request.PageSize > 100)
                request.PageSize = 20;

            // Search patients
            var (patients, totalCount) = await _patientRepository.SearchPatientsAsync(
                request.MRNo,
                request.Name,
                request.EmiratesIDN,
                request.CellPhone,
                request.PageNumber,
                request.PageSize);

            // Map to DTOs
            var patientDtos = patients.Select(p => new PatientListItemDto
            {
                MRNo = p.MRNo,
                PersonFirstName = p.PersonFirstName,
                PersonMiddleName = p.PersonMiddleName,
                PersonLastName = p.PersonLastName,
                PersonSex = p.PersonSex,
                PatientBirthDate = PatientListItemDto.ParseDbDate(p.PatientBirthDate),
                PatientBirthDateString = p.PatientBirthDate,
                PersonCellPhone = p.PersonCellPhone,
                PersonEmail = p.PersonEmail,
                PersonAddress1 = p.PersonAddress1,
                Nationality = p.Nationality,
                EmiratesIDN = p.EmiratesIDN,
                PatientFirstVisitDate = PatientListItemDto.ParseDbDate(p.PatientFirstVisitDate),
                PatientFirstVisitDateString = p.PatientFirstVisitDate,
                CreatedDate = PatientListItemDto.ParseDbDate(p.CreatedDate),
                CreatedDateString = p.CreatedDate,
                VIPPatient = p.VIPPatient,
                Inactive = p.Inactive,
                FacilityName = p.FacilityName
            }).ToList();

            var response = new PaginatedPatientResponse
            {
                Patients = patientDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            _logger.LogInformation(
                "Found {Count} patients out of {Total}",
                patientDtos.Count, totalCount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching patients");
            return StatusCode(500, new { message = "An error occurred while searching patients" });
        }
    }

    /// <summary>
    /// Get patient by MRNo
    /// </summary>
    /// <param name="mrNo">Medical Record Number</param>
    /// <returns>Patient details</returns>
    [HttpGet("{mrNo}")]
    [ProducesResponseType(typeof(PatientListItemDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPatientByMRNo(string mrNo)
    {
        try
        {
            _logger.LogInformation("Getting patient by MRNo: {MRNo}", mrNo);

            var patient = await _patientRepository.GetPatientByMRNoAsync(mrNo);

            if (patient == null)
            {
                _logger.LogWarning("Patient not found - MRNo: {MRNo}", mrNo);
                return NotFound(new { message = $"Patient with MRNo {mrNo} not found" });
            }

            var patientDto = new PatientListItemDto
            {
                MRNo = patient.MRNo,
                PersonFirstName = patient.PersonFirstName,
                PersonMiddleName = patient.PersonMiddleName,
                PersonLastName = patient.PersonLastName,
                PersonSex = patient.PersonSex,
                PatientBirthDate = PatientListItemDto.ParseDbDate(patient.PatientBirthDate),
                PatientBirthDateString = patient.PatientBirthDate,
                PersonCellPhone = patient.PersonCellPhone,
                PersonEmail = patient.PersonEmail,
                PersonAddress1 = patient.PersonAddress1,
                Nationality = patient.Nationality,
                EmiratesIDN = patient.EmiratesIDN,
                PatientFirstVisitDate = PatientListItemDto.ParseDbDate(patient.PatientFirstVisitDate),
                PatientFirstVisitDateString = patient.PatientFirstVisitDate,
                CreatedDate = PatientListItemDto.ParseDbDate(patient.CreatedDate),
                CreatedDateString = patient.CreatedDate,
                VIPPatient = patient.VIPPatient,
                Inactive = patient.Inactive,
                FacilityName = patient.FacilityName
            };

            return Ok(patientDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient by MRNo: {MRNo}", mrNo);
            return StatusCode(500, new { message = "An error occurred while retrieving patient" });
        }
    }

    /// <summary>
    /// Get all patients without filters (with pagination)
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paginated list of all patients</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedPatientResponse), 200)]
    public async Task<IActionResult> GetAllPatients(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Getting all patients - Page: {Page}, PageSize: {PageSize}", pageNumber, pageSize);

            var request = new PatientSearchRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await SearchPatients(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all patients");
            return StatusCode(500, new { message = "An error occurred while retrieving patients" });
        }
    }
}
