using Asp.Versioning;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/specialities")]
[ApiVersion("1.0")]
[Authorize]
public class CrmSpecialitiesController : ControllerBase
{
    private readonly ICrmSpecialityRepository _crmSpecialityRepository;

    public CrmSpecialitiesController(ICrmSpecialityRepository crmSpecialityRepository)
    {
        _crmSpecialityRepository = crmSpecialityRepository;
    }

    [HttpGet]
    [Permission("Specialities.Read")]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var rows = await _crmSpecialityRepository.GetAllAsync(includeInactive);
        return Ok(rows);
    }
}
