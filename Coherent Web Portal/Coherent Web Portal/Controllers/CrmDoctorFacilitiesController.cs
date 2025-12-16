using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/doctor-facilities")]
[ApiVersion("1.0")]
[Authorize]
public class CrmDoctorFacilitiesController : ControllerBase
{
    private readonly ICrmDoctorFacilityRepository _crmDoctorFacilityRepository;

    public CrmDoctorFacilitiesController(ICrmDoctorFacilityRepository crmDoctorFacilityRepository)
    {
        _crmDoctorFacilityRepository = crmDoctorFacilityRepository;
    }

    [HttpGet("{doctorId:int}")]
    [Permission("Doctors.Read")]
    public async Task<IActionResult> GetByDoctor([FromRoute] int doctorId)
    {
        var rows = await _crmDoctorFacilityRepository.GetByDoctorIdAsync(doctorId);
        return Ok(rows);
    }

    [HttpPost]
    [Permission("DoctorFacilities.Manage")]
    public async Task<IActionResult> Add([FromBody] CrmDoctorFacilityUpsertRequest request)
    {
        await _crmDoctorFacilityRepository.AddAsync(request.DoctorId, request.FacilityId);
        return NoContent();
    }

    [HttpDelete]
    [Permission("DoctorFacilities.Manage")]
    public async Task<IActionResult> Remove([FromQuery] int doctorId, [FromQuery] int facilityId)
    {
        await _crmDoctorFacilityRepository.RemoveAsync(doctorId, facilityId);
        return NoContent();
    }
}
