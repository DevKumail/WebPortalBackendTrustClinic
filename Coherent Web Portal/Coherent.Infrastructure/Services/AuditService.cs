using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Coherent.Infrastructure.Data;
using Coherent.Infrastructure.Repositories;

namespace Coherent.Infrastructure.Services;

/// <summary>
/// ADHICS Compliance: Comprehensive audit logging service
/// </summary>
public class AuditService : IAuditService
{
    private readonly DatabaseConnectionFactory _connectionFactory;

    public AuditService(DatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task LogAsync(AuditLog auditLog)
    {
        try
        {
            using var connection = _connectionFactory.CreatePrimaryConnection();
            var repository = new AuditLogRepository(connection);
            await repository.AddAsync(auditLog);
        }
        catch (Exception ex)
        {
            // Log to file or alternative system if database logging fails
            Console.WriteLine($"Failed to log audit: {ex.Message}");
        }
    }

    public async Task LogActionAsync(
        Guid? userId,
        string username,
        string action,
        string entityType,
        string? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string ipAddress = "",
        string userAgent = "",
        string databaseSource = "",
        string complianceCategory = "",
        string riskLevel = "Low",
        bool isSuccess = true,
        string? errorMessage = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Username = username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            DatabaseSource = databaseSource,
            ComplianceCategory = complianceCategory,
            RiskLevel = riskLevel
        };

        await LogAsync(auditLog);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime from, DateTime to, string? username = null)
    {
        using var connection = _connectionFactory.CreatePrimaryConnection();
        var repository = new AuditLogRepository(connection);
        return await repository.GetAuditLogsByDateRangeAsync(from, to, username);
    }
}
