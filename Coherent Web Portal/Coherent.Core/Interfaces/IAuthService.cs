using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, string ipAddress, string userAgent);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent);
    Task<bool> LogoutAsync(Guid userId);
    Task<bool> ValidateTokenAsync(string token);
    Task<UserDto?> GetCurrentUserAsync(Guid userId);
}
