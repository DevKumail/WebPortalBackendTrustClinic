using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

/// <summary>
/// ADHICS Compliance: Audit logging service
/// </summary>
public interface IAuditService
{
    Task LogAsync(AuditLog auditLog);
    Task LogActionAsync(
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
        string? errorMessage = null);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime from, DateTime to, string? username = null);
}
