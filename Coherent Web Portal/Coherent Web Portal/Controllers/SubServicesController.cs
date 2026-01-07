using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/sub-services")]
[ApiVersion("1.0")]
[Authorize]
public class SubServicesController : ControllerBase
{
    private readonly ISubServiceRepository _subServiceRepository;
    private readonly IFacilityServiceRepository _facilityServiceRepository;
    private readonly ILogger<SubServicesController> _logger;

    public SubServicesController(
        ISubServiceRepository subServiceRepository,
        IFacilityServiceRepository facilityServiceRepository,
        ILogger<SubServicesController> logger)
    {
        _subServiceRepository = subServiceRepository;
        _facilityServiceRepository = facilityServiceRepository;
        _logger = logger;
    }

    [HttpGet]
    [Permission("FacilityServices.Read")]
    public async Task<IActionResult> GetAll([FromQuery] int? serviceId = null, [FromQuery] bool includeInactive = false)
    {
        var rows = await _subServiceRepository.GetAllAsync(serviceId, includeInactive);
        return Ok(rows);
    }

    [HttpGet("{subServiceId:int}")]
    [Permission("FacilityServices.Read")]
    public async Task<IActionResult> GetById([FromRoute] int subServiceId)
    {
        var row = await _subServiceRepository.GetByIdAsync(subServiceId);
        if (row == null)
            return NotFound(new { message = $"Sub-service with ID {subServiceId} not found" });

        return Ok(row);
    }

    /// <summary>
    /// Create a new Sub-Service
    /// </summary>
    /// <param name="sId">Parent Service ID (required)</param>
    /// <param name="fId">Facility ID (optional)</param>
    /// <param name="subServiceTitle">English title</param>
    /// <param name="arSubServiceTitle">Arabic title</param>
    /// <param name="details">English details (rich text)</param>
    /// <param name="arDetails">Arabic details (rich text)</param>
    /// <param name="displayOrder">Display order</param>
    /// <param name="active">Is active</param>
    [HttpPost]
    [Permission("FacilityServices.Manage")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] int sId,
        [FromForm] int? fId,
        [FromForm] string? subServiceTitle,
        [FromForm] string? arSubServiceTitle,
        [FromForm] string? details,
        [FromForm] string? arDetails,
        [FromForm] int? displayOrder,
        [FromForm] bool? active)
    {
        var parentService = await _facilityServiceRepository.GetByIdAsync(sId);
        if (parentService == null)
            return BadRequest(new { message = $"Parent service with ID {sId} not found" });

        var request = new SubServiceUpsertRequest
        {
            SSId = null,
            SId = sId,
            FId = fId ?? parentService.FId,
            SubServiceTitle = subServiceTitle,
            ArSubServiceTitle = arSubServiceTitle,
            Details = details,
            ArDetails = arDetails,
            DisplayOrder = displayOrder,
            Active = active ?? true
        };

        var id = await _subServiceRepository.UpsertAsync(request);
        var created = await _subServiceRepository.GetByIdAsync(id);
        return Ok(new { subServiceId = id, row = created });
    }

    /// <summary>
    /// Update an existing Sub-Service
    /// </summary>
    /// <param name="subServiceId">Sub-Service ID to update</param>
    /// <param name="sId">Parent Service ID</param>
    /// <param name="fId">Facility ID</param>
    /// <param name="subServiceTitle">English title</param>
    /// <param name="arSubServiceTitle">Arabic title</param>
    /// <param name="details">English details (rich text)</param>
    /// <param name="arDetails">Arabic details (rich text)</param>
    /// <param name="displayOrder">Display order</param>
    /// <param name="active">Is active</param>
    [HttpPut("{subServiceId:int}")]
    [Permission("FacilityServices.Manage")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
        [FromRoute] int subServiceId,
        [FromForm] int? sId,
        [FromForm] int? fId,
        [FromForm] string? subServiceTitle,
        [FromForm] string? arSubServiceTitle,
        [FromForm] string? details,
        [FromForm] string? arDetails,
        [FromForm] int? displayOrder,
        [FromForm] bool? active)
    {
        var existing = await _subServiceRepository.GetByIdAsync(subServiceId);
        if (existing == null)
            return NotFound(new { message = $"Sub-service with ID {subServiceId} not found" });

        if (sId.HasValue)
        {
            var parentService = await _facilityServiceRepository.GetByIdAsync(sId.Value);
            if (parentService == null)
                return BadRequest(new { message = $"Parent service with ID {sId} not found" });
        }

        var request = new SubServiceUpsertRequest
        {
            SSId = subServiceId,
            SId = sId ?? existing.SId,
            FId = fId ?? existing.FId,
            SubServiceTitle = subServiceTitle ?? existing.SubServiceTitle,
            ArSubServiceTitle = arSubServiceTitle ?? existing.ArSubServiceTitle,
            Details = details ?? existing.Details,
            ArDetails = arDetails ?? existing.ArDetails,
            DisplayOrder = displayOrder ?? existing.DisplayOrder,
            Active = active ?? existing.Active
        };

        var id = await _subServiceRepository.UpsertAsync(request);
        var updated = await _subServiceRepository.GetByIdAsync(id);
        return Ok(new { subServiceId = id, row = updated });
    }

    [HttpDelete("{subServiceId:int}")]
    [Permission("FacilityServices.Manage")]
    public async Task<IActionResult> Delete([FromRoute] int subServiceId)
    {
        var existing = await _subServiceRepository.GetByIdAsync(subServiceId);
        if (existing == null)
            return NotFound(new { message = $"Sub-service with ID {subServiceId} not found" });

        var deleted = await _subServiceRepository.DeleteAsync(subServiceId);
        if (!deleted)
            return StatusCode(500, new { message = "Failed to delete sub-service" });

        return Ok(new { subServiceId });
    }
}
