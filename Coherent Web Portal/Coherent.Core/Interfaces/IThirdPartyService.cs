using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

/// <summary>
/// Third-party integration service
/// </summary>
public interface IThirdPartyService
{
    Task<ThirdPartyValidationResult> ValidateSecurityKeyAsync(string clientId, string securityKey, string ipAddress);
    Task<ThirdPartyValidationResult> ValidateSecurityKeyAsync(string securityKey); // Overload for V2 API
    Task<bool> LogRequestAsync(ThirdPartyRequestLogDto requestLog);
    Task<bool> IsEndpointAllowedAsync(Guid clientId, string endpoint);
    Task<bool> CheckRateLimitAsync(Guid clientId);
}
