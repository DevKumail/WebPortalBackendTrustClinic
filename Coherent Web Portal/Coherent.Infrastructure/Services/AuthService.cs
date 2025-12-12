using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Data;
using Coherent.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;

namespace Coherent.Infrastructure.Services;

/// <summary>
/// Authentication service implementation with JWT and RBAC
/// </summary>
public class AuthService : IAuthService
{
    private readonly DatabaseConnectionFactory _connectionFactory;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly IConfiguration _configuration;
    private readonly int _refreshTokenExpiryDays;

    public AuthService(
        DatabaseConnectionFactory connectionFactory,
        IJwtTokenService jwtTokenService,
        IEncryptionService encryptionService,
        IAuditService auditService,
        IConfiguration configuration)
    {
        _connectionFactory = connectionFactory;
        _jwtTokenService = jwtTokenService;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _configuration = configuration;
        _refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string ipAddress, string userAgent)
    {
        try
        {
            using var connection = _connectionFactory.CreatePrimaryConnection();
            if (!string.IsNullOrWhiteSpace(request.RegCode))
            {
                using var softwareCustomersConnection = _connectionFactory.CreateSoftwareCustomersConnection();
                var customerLoginInfoRepository = new CustomerLoginInfoRepository(softwareCustomersConnection);
                var token = await customerLoginInfoRepository.GetTokenByRegistrationCodeAsync(request.RegCode);

                if (string.IsNullOrWhiteSpace(token))
                {
                    await _auditService.LogActionAsync(
                        null, request.Username, "LOGIN_FAILED_HR", "HREmployee",
                        null, null, null, ipAddress, userAgent, "Primary",
                        "Authentication", "High", false, $"Token not found for RegCode {request.RegCode}");

                    return new AuthResult
                    {
                        IsSuccess = false,
                        Message = "Invalid username or password"
                    };
                }

                var hrRepository = new HREmployeeRepository(connection);
                var employee = await hrRepository.GetByUsernameAsync(request.Username);

                if (employee == null || !employee.Active)
                {
                    await _auditService.LogActionAsync(
                        null, request.Username, "LOGIN_FAILED_HR", "HREmployee",
                        null, null, null, ipAddress, userAgent, "Primary",
                        "Authentication", "Medium", false, "Invalid username or employee inactive");

                    return new AuthResult
                    {
                        IsSuccess = false,
                        Message = "Invalid username or password"
                    };
                }

                if (string.IsNullOrEmpty(employee.Pass))
                {
                    await _auditService.LogActionAsync(
                        null, request.Username, "LOGIN_FAILED_HR", "HREmployee",
                        employee.EmpId.ToString(), null, null, ipAddress, userAgent,
                        "Primary", "Authentication", "High", false, "Password not set for employee");

                    return new AuthResult
                    {
                        IsSuccess = false,
                        Message = "Invalid username or password"
                    };
                }

                var encryptedInputPassword = _encryptionService.LegacyEncryptPassword(request.Password, token!);

                if (!string.Equals(encryptedInputPassword, employee.Pass))
                {
                    await _auditService.LogActionAsync(
                        null, request.Username, "LOGIN_FAILED_HR", "HREmployee",
                        employee.EmpId.ToString(), null, null, ipAddress, userAgent,
                        "Primary", "Authentication", "High", false, "Invalid password");

                    return new AuthResult
                    {
                        IsSuccess = false,
                        Message = "Invalid username or password"
                    };
                }

                var hrUserDto = new UserDto
                {
                    Id = Guid.NewGuid(),
                    Username = employee.UserName ?? request.Username,
                    Email = employee.Email ?? string.Empty,
                    FirstName = employee.FName ?? string.Empty,
                    LastName = employee.LName ?? string.Empty,
                    PhoneNumber = employee.Phone ?? string.Empty,
                    IsActive = employee.Active,
                    Roles = new List<string> { "HREmployee" },
                    Permissions = new List<string>()
                };

                var accessTokenForHr = _jwtTokenService.GenerateAccessToken(
                    hrUserDto, hrUserDto.Roles, hrUserDto.Permissions);

                await _auditService.LogActionAsync(
                    null, hrUserDto.Username, "LOGIN_SUCCESS_HR", "HREmployee",
                    employee.EmpId.ToString(), null, null, ipAddress, userAgent,
                    "Primary", "Authentication", "Low", true);

                return new AuthResult
                {
                    IsSuccess = true,
                    Message = "Login successful",
                    AccessToken = accessTokenForHr,
                    RefreshToken = null,
                    AccessTokenExpiry = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60")),
                    RefreshTokenExpiry = null,
                    User = hrUserDto
                };
            }

            var userRepository = new UserRepository(connection);

            var user = await userRepository.GetByUsernameAsync(request.Username);
            
            if (user == null || !user.IsActive)
            {
                await _auditService.LogActionAsync(
                    null, request.Username, "LOGIN_FAILED", "User",
                    null, null, null, ipAddress, userAgent, "Primary", 
                    "Authentication", "Medium", false, "Invalid username or user inactive");
                
                return new AuthResult 
                { 
                    IsSuccess = false, 
                    Message = "Invalid username or password" 
                };
            }

            if (!_encryptionService.VerifyPassword(request.Password, user.PasswordHash))
            {
                await _auditService.LogActionAsync(
                    user.Id, user.Username, "LOGIN_FAILED", "User",
                    user.Id.ToString(), null, null, ipAddress, userAgent, 
                    "Primary", "Authentication", "High", false, "Invalid password");
                
                return new AuthResult 
                { 
                    IsSuccess = false, 
                    Message = "Invalid username or password" 
                };
            }

            var roles = (await userRepository.GetUserRolesAsync(user.Id)).ToList();
            var permissions = (await userRepository.GetUserPermissionsAsync(user.Id)).ToList();

            var accessToken = _jwtTokenService.GenerateAccessToken(
                MapToUserDto(user, roles, permissions), roles, permissions);
            
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

            await userRepository.UpdateRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry);

            await _auditService.LogActionAsync(
                user.Id, user.Username, "LOGIN_SUCCESS", "User",
                user.Id.ToString(), null, null, ipAddress, userAgent, 
                "Primary", "Authentication", "Low", true);

            return new AuthResult
            {
                IsSuccess = true,
                Message = "Login successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60")),
                RefreshTokenExpiry = refreshTokenExpiry,
                User = MapToUserDto(user, roles, permissions)
            };
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync(
                null, request.Username, "LOGIN_ERROR", "User",
                null, null, null, ipAddress, userAgent, "Primary", 
                "Authentication", "Critical", false, ex.Message);
            
            return new AuthResult 
            { 
                IsSuccess = false, 
                Message = "An error occurred during login" 
            };
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent)
    {
        try
        {
            using var connection = _connectionFactory.CreatePrimaryConnection();
            var userRepository = new UserRepository(connection);

            var user = await userRepository.GetByRefreshTokenAsync(refreshToken);
            
            if (user == null)
            {
                return new AuthResult 
                { 
                    IsSuccess = false, 
                    Message = "Invalid or expired refresh token" 
                };
            }

            var roles = (await userRepository.GetUserRolesAsync(user.Id)).ToList();
            var permissions = (await userRepository.GetUserPermissionsAsync(user.Id)).ToList();

            var newAccessToken = _jwtTokenService.GenerateAccessToken(
                MapToUserDto(user, roles, permissions), roles, permissions);
            
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

            await userRepository.UpdateRefreshTokenAsync(user.Id, newRefreshToken, refreshTokenExpiry);

            await _auditService.LogActionAsync(
                user.Id, user.Username, "TOKEN_REFRESH", "User",
                user.Id.ToString(), null, null, ipAddress, userAgent, 
                "Primary", "Authentication", "Low", true);

            return new AuthResult
            {
                IsSuccess = true,
                Message = "Token refreshed successfully",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60")),
                RefreshTokenExpiry = refreshTokenExpiry,
                User = MapToUserDto(user, roles, permissions)
            };
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync(
                null, "", "TOKEN_REFRESH_ERROR", "User",
                null, null, null, ipAddress, userAgent, "Primary", 
                "Authentication", "High", false, ex.Message);
            
            return new AuthResult 
            { 
                IsSuccess = false, 
                Message = "An error occurred during token refresh" 
            };
        }
    }

    public async Task<bool> LogoutAsync(Guid userId)
    {
        try
        {
            using var connection = _connectionFactory.CreatePrimaryConnection();
            var userRepository = new UserRepository(connection);
            
            await userRepository.UpdateRefreshTokenAsync(userId, string.Empty, DateTime.UtcNow);
            
            var user = await userRepository.GetByIdAsync(userId);
            await _auditService.LogActionAsync(
                userId, user?.Username ?? "", "LOGOUT", "User",
                userId.ToString(), null, null, "", "", "Primary", 
                "Authentication", "Low", true);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var principal = _jwtTokenService.ValidateToken(token);
        return principal != null;
    }

    public async Task<UserDto?> GetCurrentUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreatePrimaryConnection();
        var userRepository = new UserRepository(connection);
        
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            return null;

        var roles = (await userRepository.GetUserRolesAsync(user.Id)).ToList();
        var permissions = (await userRepository.GetUserPermissionsAsync(user.Id)).ToList();

        return MapToUserDto(user, roles, permissions);
    }

    private UserDto MapToUserDto(Domain.Entities.User user, List<string> roles, List<string> permissions)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            Roles = roles,
            Permissions = permissions
        };
    }
}
