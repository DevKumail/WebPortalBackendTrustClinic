using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, string ipAddress, string userAgent);
    Task<(bool IsSuccess, bool AlreadyLoggedOut)> LogoutAsync(string token, string username);
    Task<bool> ValidateTokenAsync(string token);
    Task<UserDto?> GetCurrentUserAsync(string username, long? empId);
}
