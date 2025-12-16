using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/facilities")]
[ApiVersion("1.0")]
[Authorize]
public class CrmFacilitiesController : ControllerBase
{
    private readonly ICrmFacilityRepository _crmFacilityRepository;

    public CrmFacilitiesController(ICrmFacilityRepository crmFacilityRepository)
    {
        _crmFacilityRepository = crmFacilityRepository;
    }

    [HttpGet]
    [Permission("Facilities.Read")]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var rows = await _crmFacilityRepository.GetAllAsync(includeInactive);
        return Ok(rows);
    }

    [HttpGet("{facilityId:int}")]
    [Permission("Facilities.Read")]
    public async Task<IActionResult> GetById([FromRoute] int facilityId)
    {
        var row = await _crmFacilityRepository.GetByIdAsync(facilityId);
        if (row == null)
            return NotFound(new { message = $"Facility with ID {facilityId} not found" });

        return Ok(row);
    }

    [HttpPost]
    [Permission("Facilities.Manage")]
    public async Task<IActionResult> Create([FromBody] CrmFacilityUpsertRequest request)
    {
        request.FId = null;
        var id = await _crmFacilityRepository.UpsertAsync(request);
        return Ok(new { facilityId = id });
    }

    [HttpPut("{facilityId:int}")]
    [Permission("Facilities.Manage")]
    public async Task<IActionResult> Update([FromRoute] int facilityId, [FromBody] CrmFacilityUpsertRequest request)
    {
        request.FId = facilityId;
        var id = await _crmFacilityRepository.UpsertAsync(request);
        return Ok(new { facilityId = id });
    }
}
