using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v1/crm-users")]
[Authorize]
public class CRMUserController : ControllerBase
{
    private readonly ICRMUserRepository _repository;
    private readonly ILogger<CRMUserController> _logger;

    public CRMUserController(ICRMUserRepository repository, ILogger<CRMUserController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CRMUserListResponse), 200)]
    public async Task<IActionResult> GetCRMUsers([FromQuery] int? empType, [FromQuery] bool? isCRM, [FromQuery] int limit = 100)
    {
        try
        {
            var result = await _repository.GetCRMUsersAsync(empType, isCRM, limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching CRM users. EmpType={EmpType}, IsCRM={IsCRM}, Limit={Limit}", empType, isCRM, limit);
            return StatusCode(500, new { success = false, message = "An error occurred while fetching CRM users." });
        }
    }

    [HttpPut("{empId}/is-crm")]
    [ProducesResponseType(typeof(UpdateIsCRMResponse), 200)]
    public async Task<IActionResult> UpdateIsCRM([FromRoute] long empId, [FromBody] UpdateIsCRMRequest request)
    {
        try
        {
            var result = await _repository.UpdateIsCRMAsync(empId, request.IsCRM);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating IsCRM for EmpId={EmpId}", empId);
            return StatusCode(500, new UpdateIsCRMResponse { Success = false, Message = "An error occurred while updating CRM status." });
        }
    }

    [HttpPost("bulk-update-is-crm")]
    [ProducesResponseType(typeof(UpdateIsCRMResponse), 200)]
    public async Task<IActionResult> BulkUpdateIsCRM([FromBody] BulkUpdateIsCRMRequest request)
    {
        try
        {
            if (request.EmpIds == null || request.EmpIds.Count == 0)
            {
                return BadRequest(new UpdateIsCRMResponse
                {
                    Success = false,
                    Message = "No employee IDs provided"
                });
            }

            var result = await _repository.BulkUpdateIsCRMAsync(request.EmpIds, request.IsCRM);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating IsCRM for {Count} employees", request.EmpIds?.Count ?? 0);
            return StatusCode(500, new UpdateIsCRMResponse { Success = false, Message = "An error occurred while bulk updating CRM status." });
        }
    }

    [HttpPut("{empId}/role")]
    [ProducesResponseType(typeof(UpdateRoleIdResponse), 200)]
    public async Task<IActionResult> UpdateRoleId([FromRoute] long empId, [FromBody] UpdateRoleIdRequest request)
    {
        try
        {
            var result = await _repository.UpdateRoleIdAsync(empId, request.RoleId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating RoleId for EmpId={EmpId}", empId);
            return StatusCode(500, new UpdateRoleIdResponse { Success = false, Message = "An error occurred while updating role." });
        }
    }

    [HttpGet("emp-types")]
    [ProducesResponseType(typeof(List<object>), 200)]
    public IActionResult GetEmpTypes()
    {
        var empTypes = new List<object>
        {
            new { Id = 1, Name = "Doctor/Provider" },
            new { Id = 2, Name = "Nurse" },
            new { Id = 3, Name = "Receptionist" },
            new { Id = 4, Name = "IVFLab" },
            new { Id = 5, Name = "Admin" }
        };
        return Ok(empTypes);
    }
}
