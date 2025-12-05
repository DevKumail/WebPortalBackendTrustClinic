using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Coherent_Web_Portal.Controllers;

/// <summary>
/// Third-party API endpoints for external system integration
/// Authentication and authorization handled by ThirdPartyAuthMiddleware
/// </summary>
[ApiController]
[Route("api/third-party")]
public class ThirdPartyController : ControllerBase
{
    private readonly IAuditService _auditService;

    public ThirdPartyController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Sample endpoint for third-party data access
    /// </summary>
    [HttpPost("data")]
    public async Task<IActionResult> GetData([FromBody] object request)
    {
        // Get validated client info from middleware
        var clientInfo = HttpContext.Items["ThirdPartyClient"];
        
        // Process request based on client's data access level
        // This is a sample implementation
        
        return Ok(new 
        { 
            message = "Data retrieved successfully",
            timestamp = DateTime.UtcNow,
            client = clientInfo
        });
    }

    /// <summary>
    /// Health check endpoint for third-party systems
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new 
        { 
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }
}
