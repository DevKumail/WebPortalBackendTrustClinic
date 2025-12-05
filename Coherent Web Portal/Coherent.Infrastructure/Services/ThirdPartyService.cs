using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Coherent.Infrastructure.Data;
using Coherent.Infrastructure.Repositories;
using System.Text.Json;

namespace Coherent.Infrastructure.Services;

/// <summary>
/// Third-party integration service with security key validation
/// </summary>
public class ThirdPartyService : IThirdPartyService
{
    private readonly DatabaseConnectionFactory _connectionFactory;
    private readonly IEncryptionService _encryptionService;

    public ThirdPartyService(
        DatabaseConnectionFactory connectionFactory,
        IEncryptionService encryptionService)
    {
        _connectionFactory = connectionFactory;
        _encryptionService = encryptionService;
    }

    public async Task<ThirdPartyValidationResult> ValidateSecurityKeyAsync(
        string clientId, 
        string securityKey, 
        string ipAddress)
    {
        try
        {
            using var connection = _connectionFactory.CreateSecondaryConnection();
            var repository = new ThirdPartyClientRepository(connection);

            var client = await repository.GetByClientIdAsync(clientId);
            
            if (client == null)
            {
                return new ThirdPartyValidationResult
                {
                    IsValid = false,
                    Message = "Invalid client ID"
                };
            }

            if (!client.IsActive)
            {
                return new ThirdPartyValidationResult
                {
                    IsValid = false,
                    Message = "Client is inactive"
                };
            }

            if (client.SecurityKeyExpiry < DateTime.UtcNow)
            {
                return new ThirdPartyValidationResult
                {
                    IsValid = false,
                    Message = "Security key has expired"
                };
            }

            if (!_encryptionService.VerifySecurityKey(securityKey, client.SecurityKeyHash))
            {
                return new ThirdPartyValidationResult
                {
                    IsValid = false,
                    Message = "Invalid security key"
                };
            }

            if (!await repository.IsIpWhitelistedAsync(client.Id, ipAddress))
            {
                return new ThirdPartyValidationResult
                {
                    IsValid = false,
                    Message = "IP address not whitelisted"
                };
            }

            await repository.UpdateLastAccessAsync(client.Id);

            return new ThirdPartyValidationResult
            {
                IsValid = true,
                Message = "Validation successful",
                ClientId = client.Id,
                ClientName = client.ClientName,
                DataAccessLevel = client.DataAccessLevel
            };
        }
        catch (Exception ex)
        {
            return new ThirdPartyValidationResult
            {
                IsValid = false,
                Message = $"Validation error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validate security key only (for V2 API) - finds client by security key
    /// </summary>
    public async Task<ThirdPartyValidationResult> ValidateSecurityKeyAsync(string securityKey)
    {
        try
        {
            using var connection = _connectionFactory.CreateSecondaryConnection();
            var repository = new ThirdPartyClientRepository(connection);

            // Find client by security key
            var client = await repository.GetBySecurityKeyAsync(securityKey);
            
            if (client == null)
            {
                return new ThirdPartyValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid security key",
                    Message = "Security key not found"
                };
            }

            if (!client.IsActive)
            {
                return new ThirdPartyValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Client is inactive",
                    Message = "This client account has been deactivated"
                };
            }

            // Check if security key matches
            if (client.SecurityKey != securityKey)
            {
                return new ThirdPartyValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Security key mismatch",
                    Message = "Invalid security key"
                };
            }

            return new ThirdPartyValidationResult
            {
                IsValid = true,
                Message = "Valid security key",
                ClientId = client.Id,
                ClientName = client.ClientName,
                DataAccessLevel = client.DataAccessLevel ?? "Standard",
                AllowedIPs = client.IpWhitelist
            };
        }
        catch (Exception ex)
        {
            return new ThirdPartyValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Validation error: {ex.Message}",
                Message = "An error occurred during validation"
            };
        }
    }

    public async Task<bool> LogRequestAsync(ThirdPartyRequestLogDto requestLog)
    {
        try
        {
            using var connection = _connectionFactory.CreateSecondaryConnection();
            var repository = new ThirdPartyRequestLogRepository(connection);

            var log = new ThirdPartyRequestLog
            {
                Id = Guid.NewGuid(),
                ThirdPartyClientId = requestLog.ThirdPartyClientId,
                ClientName = requestLog.ClientName,
                Endpoint = requestLog.Endpoint,
                HttpMethod = requestLog.HttpMethod,
                RequestPayload = requestLog.RequestPayload,
                ResponsePayload = requestLog.ResponsePayload,
                StatusCode = requestLog.StatusCode,
                IpAddress = requestLog.IpAddress,
                RequestTimestamp = requestLog.RequestTimestamp,
                ResponseTimestamp = requestLog.ResponseTimestamp,
                DurationMs = requestLog.DurationMs,
                IsSuccess = requestLog.IsSuccess,
                ErrorMessage = requestLog.ErrorMessage,
                SecurityValidationResult = requestLog.SecurityValidationResult
            };

            await repository.AddAsync(log);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsEndpointAllowedAsync(Guid clientId, string endpoint)
    {
        using var connection = _connectionFactory.CreateSecondaryConnection();
        var repository = new ThirdPartyClientRepository(connection);

        var client = await repository.GetByIdAsync(clientId);
        if (client == null)
            return false;

        var allowedEndpoints = JsonSerializer.Deserialize<List<string>>(client.AllowedEndpoints) ?? new List<string>();
        return allowedEndpoints.Contains(endpoint) || allowedEndpoints.Contains("*");
    }

    public async Task<bool> CheckRateLimitAsync(Guid clientId)
    {
        using var connection = _connectionFactory.CreateSecondaryConnection();
        var requestLogRepo = new ThirdPartyRequestLogRepository(connection);
        var clientRepo = new ThirdPartyClientRepository(connection);

        var client = await clientRepo.GetByIdAsync(clientId);
        if (client == null)
            return false;

        var requestCount = await requestLogRepo.GetRequestCountInLastMinuteAsync(clientId);
        return requestCount < client.MaxRequestsPerMinute;
    }
}
