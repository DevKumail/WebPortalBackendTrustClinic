using Coherent.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Coherent.Infrastructure.Authorization;

/// <summary>
/// Custom authorization attribute for third-party/mobile app security key authentication
/// Validates X-Security-Key header against ThirdPartyClients table
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ThirdPartyAuthAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // If endpoint explicitly allows anonymous access, skip security-key validation.
        // This enables selective anonymous access even when [ThirdPartyAuth] is applied at controller level.
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        // Get security key from header
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Security-Key", out var securityKey) ||
            string.IsNullOrWhiteSpace(securityKey))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "Security key is required. Please provide X-Security-Key header."
            });
            return;
        }

        // Get third-party service
        var thirdPartyService = context.HttpContext.RequestServices.GetService<IThirdPartyService>();
        if (thirdPartyService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Get client IP
        var clientIP = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Validate security key
        var validationResult = await thirdPartyService.ValidateSecurityKeyAsync(securityKey!);

        if (!validationResult.IsValid)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = validationResult.ErrorMessage ?? "Invalid security key"
            });
            return;
        }

        // Check IP whitelist if configured
        if (!string.IsNullOrEmpty(validationResult.AllowedIPs))
        {
            var allowedIPs = validationResult.AllowedIPs.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(ip => ip.Trim())
                .ToList();

            if (allowedIPs.Any() && !allowedIPs.Contains(clientIP))
            {
                context.Result = new ForbidResult();
                await thirdPartyService.LogRequestAsync(new Core.DTOs.ThirdPartyRequestLogDto
                {
                    ClientName = validationResult.ClientName,
                    Endpoint = context.HttpContext.Request.Path,
                    IpAddress = clientIP,
                    StatusCode = 403,
                    ErrorMessage = "IP address not whitelisted",
                    ThirdPartyClientId = validationResult.ClientId ?? Guid.Empty,
                    HttpMethod = context.HttpContext.Request.Method,
                    RequestTimestamp = DateTime.UtcNow,
                    IsSuccess = false
                });
                return;
            }
        }

        // Store client info in HttpContext for logging
        context.HttpContext.Items["ThirdPartyClient"] = validationResult.ClientName;
        context.HttpContext.Items["ThirdPartyClientId"] = validationResult.ClientId;
    }
}
