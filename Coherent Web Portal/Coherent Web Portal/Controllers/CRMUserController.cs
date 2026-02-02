using Coherent.Core.DTOs;
using Coherent.Infrastructure.Data;
using Coherent.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v1/crm-users")]
[Authorize]
public class CRMUserController : ControllerBase
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ILogger<CRMUserController> _logger;

    public CRMUserController(IDatabaseConnectionFactory connectionFactory, ILogger<CRMUserController> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CRMUserListResponse), 200)]
    public async Task<IActionResult> GetCRMUsers([FromQuery] int? empType, [FromQuery] bool? isCRM, [FromQuery] int limit = 100)
    {
        using var connection = _connectionFactory.CreatePrimaryConnection();
        var repository = new HREmployeeRepository(connection);

        var employees = await repository.GetCRMUsersAsync(empType, isCRM, limit);

        var users = employees.Select(e => new CRMUserDto
        {
            EmpId = e.EmpId,
            FullName = $"{e.FName} {e.LName}".Trim(),
            FName = e.FName,
            LName = e.LName,
            Email = e.Email,
            Phone = e.Phone,
            UserName = e.UserName,
            EmpType = e.EmpType,
            EmpTypeName = GetEmpTypeName(e.EmpType),
            Speciality = e.Speciality,
            DepartmentID = e.DepartmentID,
            IsCRM = e.IsCRM,
            Active = e.Active
        }).ToList();

        return Ok(new CRMUserListResponse
        {
            Users = users,
            TotalCount = users.Count
        });
    }

    [HttpPut("{empId}/is-crm")]
    [ProducesResponseType(typeof(UpdateIsCRMResponse), 200)]
    public async Task<IActionResult> UpdateIsCRM([FromRoute] long empId, [FromBody] UpdateIsCRMRequest request)
    {
        request.EmpId = empId;

        using var connection = _connectionFactory.CreatePrimaryConnection();
        var repository = new HREmployeeRepository(connection);

        var success = await repository.UpdateIsCRMAsync(request.EmpId, request.IsCRM);

        return Ok(new UpdateIsCRMResponse
        {
            Success = success,
            Message = success ? "IsCRM updated successfully" : "Employee not found",
            AffectedCount = success ? 1 : 0
        });
    }

    [HttpPost("bulk-update-is-crm")]
    [ProducesResponseType(typeof(UpdateIsCRMResponse), 200)]
    public async Task<IActionResult> BulkUpdateIsCRM([FromBody] BulkUpdateIsCRMRequest request)
    {
        if (request.EmpIds == null || request.EmpIds.Count == 0)
        {
            return BadRequest(new UpdateIsCRMResponse
            {
                Success = false,
                Message = "No employee IDs provided"
            });
        }

        using var connection = _connectionFactory.CreatePrimaryConnection();
        var repository = new HREmployeeRepository(connection);

        var affected = await repository.BulkUpdateIsCRMAsync(request.EmpIds, request.IsCRM);

        return Ok(new UpdateIsCRMResponse
        {
            Success = affected > 0,
            Message = $"Updated {affected} employees",
            AffectedCount = affected
        });
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

    private static string GetEmpTypeName(int empType) => empType switch
    {
        1 => "Doctor/Provider",
        2 => "Nurse",
        3 => "Receptionist",
        4 => "IVFLab",
        5 => "Admin",
        _ => "Unknown"
    };
}
