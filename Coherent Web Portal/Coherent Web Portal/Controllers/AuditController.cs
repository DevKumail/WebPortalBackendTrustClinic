using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent_Web_Portal.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Get audit logs for a specific date range (ADHICS compliance)
    /// Requires Admin or Auditor role
    /// </summary>
    [HttpGet("logs")]
    [Role("Admin", "Auditor")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] DateTime from, 
        [FromQuery] DateTime to, 
        [FromQuery] string? username = null)
    {
        var logs = await _auditService.GetAuditLogsAsync(from, to, username);
        return Ok(logs);
    }
}
