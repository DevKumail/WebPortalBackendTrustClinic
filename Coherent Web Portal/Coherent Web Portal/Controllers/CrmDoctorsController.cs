using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/doctors")]
[ApiVersion("1.0")]
[Authorize]
public class CrmDoctorsController : ControllerBase
{
    private readonly ICrmDoctorRepository _crmDoctorRepository;
    private readonly ILogger<CrmDoctorsController> _logger;

    public CrmDoctorsController(ICrmDoctorRepository crmDoctorRepository, ILogger<CrmDoctorsController> logger)
    {
        _crmDoctorRepository = crmDoctorRepository;
        _logger = logger;
    }

    [HttpGet]
    [Permission("Doctors.Read")]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var rows = await _crmDoctorRepository.GetAllAsync(includeInactive);
        return Ok(rows);
    }

    [HttpGet("{doctorId:int}")]
    [Permission("Doctors.Read")]
    public async Task<IActionResult> GetById([FromRoute] int doctorId)
    {
        var row = await _crmDoctorRepository.GetByIdAsync(doctorId);
        if (row == null)
            return NotFound(new { message = $"Doctor with ID {doctorId} not found" });

        return Ok(row);
    }

    [HttpPost]
    [Permission("Doctors.Manage")]
    public async Task<IActionResult> Create([FromBody] CrmDoctorUpsertRequest request)
    {
        request.DId = null;
        var id = await _crmDoctorRepository.UpsertAsync(request);
        return Ok(new { doctorId = id });
    }

    [HttpPut("{doctorId:int}")]
    [Permission("Doctors.Manage")]
    public async Task<IActionResult> Update([FromRoute] int doctorId, [FromBody] CrmDoctorUpsertRequest request)
    {
        request.DId = doctorId;
        var id = await _crmDoctorRepository.UpsertAsync(request);
        return Ok(new { doctorId = id });
    }
}
