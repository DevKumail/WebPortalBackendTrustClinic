using Coherent.Core.DTOs;
using System.Security.Claims;

namespace Coherent.Core.Interfaces;

/// <summary>
/// JWT Token generation and validation service
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(UserDto user, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    Guid? GetUserIdFromToken(string token);
    IEnumerable<string> GetRolesFromToken(string token);
    IEnumerable<string> GetPermissionsFromToken(string token);
}
