using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Data;
using Dapper;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

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

    public async Task InvokeAsync(HttpContext context, IJwtTokenService jwtTokenService, IAuditService auditService, DatabaseConnectionFactory connectionFactory)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (!string.IsNullOrEmpty(token))
        {
            var principal = jwtTokenService.ValidateToken(token);
            
            if (principal != null)
            {
                try
                {
                    var tokenHash = ComputeSha256Hex(token);
                    using var connection = connectionFactory.CreatePrimaryConnection();
                    var isRevoked = await connection.QueryFirstOrDefaultAsync<int>(
                        "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.SecAuthSession WHERE TokenHash = @TokenHash AND IsLoggedOut = 1) THEN 1 ELSE 0 END",
                        new { TokenHash = tokenHash }) == 1;

                    if (isRevoked)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Token has been logged out");
                        return;
                    }
                }
                catch
                {
                    // If revocation check fails, fall back to normal JWT behavior.
                }

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

    private static string ComputeSha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
