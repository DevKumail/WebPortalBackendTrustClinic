using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/patient-education-assignments")]
[ApiVersion("1.0")]
[Authorize]
public class PatientEducationAssignmentController : ControllerBase
{
    private readonly IPatientEducationAssignmentRepository _assignmentRepository;
    private readonly IPatientEducationRepository _educationRepository;
    private readonly ILogger<PatientEducationAssignmentController> _logger;

    public PatientEducationAssignmentController(
        IPatientEducationAssignmentRepository assignmentRepository,
        IPatientEducationRepository educationRepository,
        ILogger<PatientEducationAssignmentController> logger)
    {
        _assignmentRepository = assignmentRepository;
        _educationRepository = educationRepository;
        _logger = logger;
    }

    #region URL Builders

    private string? BuildThumbnailUrl(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        if (Uri.TryCreate(fileName, UriKind.Absolute, out _)) return fileName;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/images/education/thumbnails/{fileName.TrimStart('/')}";
    }

    private string? BuildPdfUrl(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        if (Uri.TryCreate(fileName, UriKind.Absolute, out _)) return fileName;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/files/education/pdfs/{fileName.TrimStart('/')}";
    }


    #endregion

    #region Assignment Endpoints

    /// <summary>
    /// Get all education assignments for a patient
    /// </summary>
    [HttpGet("by-patient")]
    [Permission("PatientEducation.Read")]
    public async Task<IActionResult> GetAssignmentsByPatient(
        [FromQuery] string? mrNo,
        [FromQuery] bool includeExpired = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (assignments, totalCount) = await _assignmentRepository.GetAssignmentsAsync(
            mrNo,
            includeExpired,
            pageNumber,
            pageSize);

        var response = new PaginatedPatientEducationAssignmentResponse
        {
            Assignments = assignments.ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Get all patient assignments for an education content
    /// </summary>
    [HttpGet("by-education/{educationId:int}")]
    [Permission("PatientEducation.Read")]
    public async Task<IActionResult> GetAssignmentsByEducation([FromRoute] int educationId)
    {
        var assignments = await _assignmentRepository.GetByEducationIdAsync(educationId);
        return Ok(assignments);
    }

    /// <summary>
    /// Get assignment details by ID
    /// </summary>
    [HttpGet("{assignmentId:int}")]
    [Permission("PatientEducation.Read")]
    public async Task<IActionResult> GetAssignmentById([FromRoute] int assignmentId)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
        if (assignment == null)
            return NotFound(new { message = $"Assignment with ID {assignmentId} not found" });

        // Get full education details
        if (assignment.EducationId > 0)
        {
            var education = await _educationRepository.GetDetailByIdAsync(assignment.EducationId);
            if (education != null)
            {
                education.ThumbnailImageUrl = BuildThumbnailUrl(education.ThumbnailImageName);
                education.PdfFileUrl = BuildPdfUrl(education.PdfFileUrl);
                assignment.Education = education;
            }
        }

        return Ok(assignment);
    }

    /// <summary>
    /// Assign education to a patient
    /// </summary>
    [HttpPost]
    [Permission("PatientEducation.Manage")]
    public async Task<IActionResult> AssignEducation([FromBody] PatientEducationAssignmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MRNo))
            return BadRequest(new { message = "MRNo is required" });

        if (request.EducationId <= 0)
            return BadRequest(new { message = "EducationId is required" });

        // Verify education exists
        var education = await _educationRepository.GetByIdAsync(request.EducationId);
        if (education == null)
            return NotFound(new { message = $"Education with ID {request.EducationId} not found" });

        // Get current user ID from claims
        var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
        int? assignedByUserId = int.TryParse(userIdClaim, out var uid) ? uid : null;

        var assignmentId = await _assignmentRepository.AssignAsync(request, assignedByUserId);

        return Ok(new 
        { 
            assignmentId, 
            mrNo = request.MRNo,
            educationId = request.EducationId,
            message = "Education assigned successfully" 
        });
    }

    /// <summary>
    /// Bulk assign education to multiple patients
    /// </summary>
    [HttpPost("bulk")]
    [Permission("PatientEducation.Manage")]
    public async Task<IActionResult> BulkAssignEducation([FromBody] PatientEducationBulkAssignRequest request)
    {
        if (request.MRNos == null || request.MRNos.Count == 0)
            return BadRequest(new { message = "MRNos are required" });

        if (request.EducationId <= 0)
            return BadRequest(new { message = "EducationId is required" });

        // Verify education exists
        var education = await _educationRepository.GetByIdAsync(request.EducationId);
        if (education == null)
            return NotFound(new { message = $"Education with ID {request.EducationId} not found" });

        // Get current user ID from claims
        var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
        int? assignedByUserId = int.TryParse(userIdClaim, out var uid) ? uid : null;

        var assignmentIds = await _assignmentRepository.BulkAssignAsync(request, assignedByUserId);

        return Ok(new 
        { 
            assignmentIds, 
            patientCount = request.MRNos.Count,
            educationId = request.EducationId,
            message = $"Education assigned to {request.MRNos.Count} patients successfully" 
        });
    }

    /// <summary>
    /// Mark assignment as viewed by patient
    /// </summary>
    [HttpPost("{assignmentId:int}/viewed")]
    [Permission("PatientEducation.Read")]
    public async Task<IActionResult> MarkAsViewed([FromRoute] int assignmentId)
    {
        var updated = await _assignmentRepository.MarkAsViewedAsync(assignmentId);
        if (!updated)
            return NotFound(new { message = $"Assignment with ID {assignmentId} not found" });

        return Ok(new { assignmentId, message = "Marked as viewed" });
    }

    /// <summary>
    /// Update assignment (notes, expiry, active status)
    /// </summary>
    [HttpPut("{assignmentId:int}")]
    [Permission("PatientEducation.Manage")]
    public async Task<IActionResult> UpdateAssignment(
        [FromRoute] int assignmentId,
        [FromBody] PatientEducationAssignmentRequest request)
    {
        var updated = await _assignmentRepository.UpdateAsync(
            assignmentId, 
            request.Notes, 
            request.ArNotes, 
            request.ExpiresAt, 
            null);

        if (!updated)
            return NotFound(new { message = $"Assignment with ID {assignmentId} not found" });

        return Ok(new { assignmentId, message = "Assignment updated successfully" });
    }

    /// <summary>
    /// Delete assignment
    /// </summary>
    [HttpDelete("{assignmentId:int}")]
    [Permission("PatientEducation.Manage")]
    public async Task<IActionResult> DeleteAssignment([FromRoute] int assignmentId)
    {
        var deleted = await _assignmentRepository.DeleteAsync(assignmentId);
        if (!deleted)
            return NotFound(new { message = $"Assignment with ID {assignmentId} not found" });

        return Ok(new { assignmentId, message = "Assignment deleted successfully" });
    }

    /// <summary>
    /// Remove education from patient (by patientId and educationId)
    /// </summary>
    [HttpDelete("patient/{mrNo}/education/{educationId:int}")]
    [Permission("PatientEducation.Manage")]
    public async Task<IActionResult> RemoveAssignment([FromRoute] string mrNo, [FromRoute] int educationId)
    {
        var deleted = await _assignmentRepository.DeleteByMrNoAndEducationAsync(mrNo, educationId);
        if (!deleted)
            return NotFound(new { message = "Assignment not found" });

        return Ok(new { mrNo, educationId, message = "Assignment removed successfully" });
    }

    #endregion
}
