using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class SecurityController : ControllerBase
{
    private readonly ISecurityRepository _securityRepository;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(ISecurityRepository securityRepository, ILogger<SecurityController> logger)
    {
        _securityRepository = securityRepository;
        _logger = logger;
    }

    [HttpGet("roles")]
    [ProducesResponseType(typeof(List<SecRoleDto>), 200)]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _securityRepository.GetRolesAsync();
        return Ok(roles);
    }

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(List<SecPermissionDto>), 200)]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _securityRepository.GetPermissionsAsync();
        return Ok(permissions);
    }

    [HttpGet("roles/{roleId:int}/permissions")]
    [ProducesResponseType(typeof(List<string>), 200)]
    public async Task<IActionResult> GetRolePermissions([FromRoute] int roleId)
    {
        var permissions = await _securityRepository.GetRolePermissionKeysAsync(roleId);
        return Ok(permissions);
    }

    [HttpPut("roles/{roleId:int}/permissions")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> SetRolePermissions([FromRoute] int roleId, [FromBody] SetRolePermissionsRequest request)
    {
        var assignedBy = User?.Identity?.Name;
        await _securityRepository.SetRolePermissionsAsync(roleId, request.PermissionKeys ?? new List<string>(), assignedBy);
        return NoContent();
    }

    [HttpGet("employees/{empId:long}/permissions")]
    [ProducesResponseType(typeof(List<SecEmployeePermissionDto>), 200)]
    public async Task<IActionResult> GetEmployeePermissions([FromRoute] long empId)
    {
        var permissions = await _securityRepository.GetEmployeeEffectivePermissionsByEmpIdAsync(empId);
        return Ok(permissions);
    }

    [HttpGet("employees/by-username/{username}/permissions")]
    [ProducesResponseType(typeof(List<SecEmployeePermissionDto>), 200)]
    public async Task<IActionResult> GetEmployeePermissionsByUsername([FromRoute] string username)
    {
        var permissions = await _securityRepository.GetEmployeeEffectivePermissionsByUsernameAsync(username);
        return Ok(permissions);
    }

    [HttpPut("employees/{empId:long}/permissions")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> SetEmployeeOverrides([FromRoute] long empId, [FromBody] SetEmployeePermissionOverridesRequest request)
    {
        var assignedBy = User?.Identity?.Name;
        await _securityRepository.SetEmployeePermissionOverridesAsync(empId, request.Overrides ?? new List<SecEmployeePermissionOverrideDto>(), assignedBy);
        return NoContent();
    }
}
