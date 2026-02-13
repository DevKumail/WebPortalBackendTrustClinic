using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Coherent.Infrastructure.Data;
using Coherent.Infrastructure.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

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
    private readonly ILoggerFactory _loggerFactory;
    private readonly int _refreshTokenExpiryDays;

    public AuthService(
        DatabaseConnectionFactory connectionFactory,
        IJwtTokenService jwtTokenService,
        IEncryptionService encryptionService,
        IAuditService auditService,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        _connectionFactory = connectionFactory;
        _jwtTokenService = jwtTokenService;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _configuration = configuration;
        _loggerFactory = loggerFactory;
        _refreshTokenExpiryDays = int.Parse(configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string ipAddress, string userAgent)
    {
        try
        {
            using var connection = _connectionFactory.CreatePrimaryConnection();
            if (string.IsNullOrWhiteSpace(request.RegCode))
            {
                await _auditService.LogActionAsync(
                    null, request.Username, "LOGIN_FAILED_HR", "HREmployee",
                    null, null, null, ipAddress, userAgent, "Primary",
                    "Authentication", "Medium", false, "RegCode is required");

                return new AuthResult
                {
                    IsSuccess = false,
                    Message = "RegCode is required"
                };
            }

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

            var encryptedInputPassword = _encryptionService.LegacyEncryptPassword(request.Password, token);

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

            if (!employee.IsCRM)
            {
                await _auditService.LogActionAsync(
                    null, request.Username, "LOGIN_FAILED_HR", "HREmployee",
                    employee.EmpId.ToString(), null, null, ipAddress, userAgent,
                    "Primary", "Authentication", "Medium", false, "CRM access not enabled for this employee");

                return new AuthResult
                {
                    IsSuccess = false,
                    Message = "CRM access is not enabled for your account. Please contact your administrator."
                };
            }

            var hrUserDto = new UserDto
            {
                Id = Guid.NewGuid(),
                EmpId = employee.EmpId,
                EmpType = employee.EmpType,
                LicenseNo = employee.ProvNPI,
                Username = employee.UserName ?? request.Username,
                Email = employee.Email ?? string.Empty,
                FirstName = employee.FName ?? string.Empty,
                LastName = employee.LName ?? string.Empty,
                PhoneNumber = employee.Phone ?? string.Empty,
                IsActive = employee.Active,
                IsCRM = employee.IsCRM,
                Roles = new List<string>(),
                Permissions = new List<string>()
            };

            var securityRepository = new SecurityRepository(connection, _loggerFactory.CreateLogger<SecurityRepository>());

            if (employee.RoleId.HasValue)
            {
                var roleName = await securityRepository.GetRoleNameByRoleIdAsync(employee.RoleId.Value);
                if (!string.IsNullOrWhiteSpace(roleName))
                    hrUserDto.Roles.Add(roleName);
            }

            if (hrUserDto.Roles.Count == 0)
                hrUserDto.Roles.Add("HREmployee");

            var effectivePermissions = await securityRepository.GetEmployeeEffectivePermissionsByEmpIdAsync(employee.EmpId);
            hrUserDto.Permissions = effectivePermissions
                .Where(p => p.IsAllowed)
                .Select(p => p.PermissionKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var accessTokenForHr = _jwtTokenService.GenerateAccessToken(
                hrUserDto, hrUserDto.Roles, hrUserDto.Permissions);

            try
            {
                var issuedAt = DateTime.UtcNow;
                var expiresAt = issuedAt.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60"));
                var tokenHash = ComputeSha256Hex(accessTokenForHr);
                var tokenLast8 = accessTokenForHr.Length >= 8 ? accessTokenForHr.Substring(accessTokenForHr.Length - 8) : accessTokenForHr;

                var rolesCsv = hrUserDto.Roles != null && hrUserDto.Roles.Count > 0
                    ? string.Join(",", hrUserDto.Roles)
                    : null;

                var permissionsCsv = hrUserDto.Permissions != null && hrUserDto.Permissions.Count > 0
                    ? string.Join(",", hrUserDto.Permissions)
                    : null;

                var insertSql = @"
INSERT INTO dbo.SecAuthSession
(
    EmpId, Username, RegCode, TokenHash, TokenLast8,
    IssuedAt, ExpiresAt, IpAddress, UserAgent,
    RolesCsv, PermissionsCsv
)
VALUES
(
    @EmpId, @Username, @RegCode, @TokenHash, @TokenLast8,
    @IssuedAt, @ExpiresAt, @IpAddress, @UserAgent,
    @RolesCsv, @PermissionsCsv
)";

                await connection.ExecuteAsync(insertSql, new
                {
                    EmpId = employee.EmpId,
                    Username = hrUserDto.Username,
                    RegCode = request.RegCode,
                    TokenHash = tokenHash,
                    TokenLast8 = tokenLast8,
                    IssuedAt = issuedAt,
                    ExpiresAt = expiresAt,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    RolesCsv = rolesCsv,
                    PermissionsCsv = permissionsCsv
                });
            }
            catch (Exception ex)
            {
                await _auditService.LogActionAsync(
                    null, hrUserDto.Username, "AUTH_SESSION_LOG_FAILED", "SecAuthSession",
                    employee.EmpId.ToString(), null, null, ipAddress, userAgent,
                    "Primary", "Authentication", "Low", false, ex.Message);
            }

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

    public async Task<(bool IsSuccess, bool AlreadyLoggedOut)> LogoutAsync(string token, string username)
    {
        try
        {
            using var connection = _connectionFactory.CreatePrimaryConnection();
            if (string.IsNullOrWhiteSpace(token))
                return (false, false);

            var tokenHash = ComputeSha256Hex(token);
            var tokenLast8 = token.Length >= 8 ? token.Substring(token.Length - 8) : token;

            var alreadyLoggedOut = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.SecAuthSession WHERE TokenHash = @TokenHash AND IsLoggedOut = 1) THEN 1 ELSE 0 END",
                new { TokenHash = tokenHash }) == 1;

            if (!alreadyLoggedOut)
            {
                var updated = await connection.ExecuteAsync(
                    "UPDATE dbo.SecAuthSession SET IsLoggedOut = 1, LoggedOutAt = SYSUTCDATETIME() WHERE TokenHash = @TokenHash AND IsLoggedOut = 0",
                    new { TokenHash = tokenHash });

                if (updated == 0)
                {
                    // If the session wasn't logged (e.g. missing table row), insert a minimal revoked record to block further usage.
                    await connection.ExecuteAsync(
                        @"INSERT INTO dbo.SecAuthSession
                          (
                              EmpId, Username, RegCode, TokenHash, TokenLast8,
                              IssuedAt, ExpiresAt, IsLoggedOut, LoggedOutAt
                          )
                          VALUES
                          (
                              NULL, @Username, NULL, @TokenHash, @TokenLast8,
                              SYSUTCDATETIME(), SYSUTCDATETIME(), 1, SYSUTCDATETIME()
                          )",
                        new { Username = username, TokenHash = tokenHash, TokenLast8 = tokenLast8 });
                }
            }

            await _auditService.LogActionAsync(
                null, username ?? string.Empty, "LOGOUT", "SecAuthSession",
                null, null, null, "", "", "Primary",
                "Authentication", "Low", true);

            return (true, alreadyLoggedOut);
        }
        catch (Exception ex)
        {
            await _auditService.LogActionAsync(
                null, username ?? string.Empty, "LOGOUT_ERROR", "SecAuthSession",
                null, null, null, string.Empty, string.Empty, "Primary",
                "Authentication", "High", false, ex.Message);

            return (false, false);
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var principal = _jwtTokenService.ValidateToken(token);
        return principal != null;
    }

    public async Task<UserDto?> GetCurrentUserAsync(string username, long? empId)
    {
        using var connection = _connectionFactory.CreatePrimaryConnection();

        var hrRepository = new HREmployeeRepository(connection);
        HREmployee? employee = null;
        if (empId.HasValue)
            employee = await hrRepository.GetByEmpIdAsync(empId.Value);
        if (employee == null && !string.IsNullOrWhiteSpace(username))
            employee = await hrRepository.GetByUsernameAsync(username);

        if (employee == null)
            return null;

        var securityRepository = new SecurityRepository(connection, _loggerFactory.CreateLogger<SecurityRepository>());
        var roles = new List<string>();
        if (employee.RoleId.HasValue)
        {
            var roleName = await securityRepository.GetRoleNameByRoleIdAsync(employee.RoleId.Value);
            if (!string.IsNullOrWhiteSpace(roleName))
                roles.Add(roleName);
        }
        if (roles.Count == 0)
            roles.Add("HREmployee");

        var effectivePermissions = await securityRepository.GetEmployeeEffectivePermissionsByEmpIdAsync(employee.EmpId);
        var permissions = effectivePermissions
            .Where(p => p.IsAllowed)
            .Select(p => p.PermissionKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new UserDto
        {
            Id = Guid.NewGuid(),
            EmpId = employee.EmpId,
            EmpType = employee.EmpType,
            LicenseNo = employee.ProvNPI,
            Username = employee.UserName ?? username,
            Email = employee.Email ?? string.Empty,
            FirstName = employee.FName ?? string.Empty,
            LastName = employee.LName ?? string.Empty,
            PhoneNumber = employee.Phone ?? string.Empty,
            IsActive = employee.Active,
            IsCRM = employee.IsCRM,
            Roles = roles,
            Permissions = permissions
        };
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
