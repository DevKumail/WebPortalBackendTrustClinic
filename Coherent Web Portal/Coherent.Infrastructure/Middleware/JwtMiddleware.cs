using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace Coherent.Infrastructure.Middleware;

/// <summary>
/// Middleware to validate JWT tokens on each request
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;

    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJwtTokenService jwtTokenService, IAuditService auditService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (!string.IsNullOrEmpty(token))
        {
            var principal = jwtTokenService.ValidateToken(token);
            
            if (principal != null)
            {
                context.User = principal;
            }
            else
            {
                // Log failed token validation
                await auditService.LogActionAsync(
                    null,
                    context.User?.Identity?.Name ?? "Anonymous",
                    "TOKEN_VALIDATION_FAILED",
                    "Authentication",
                    null,
                    null,
                    null,
                    context.Connection.RemoteIpAddress?.ToString() ?? "",
                    context.Request.Headers["User-Agent"].ToString(),
                    "Primary",
                    "Authentication",
                    "Medium",
                    false,
                    "Invalid or expired token"
                );
            }
        }

        await _next(context);
    }
}
