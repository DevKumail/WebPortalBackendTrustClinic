namespace Coherent.Domain.Entities;

/// <summary>
/// ADHICS Compliance: Comprehensive audit logging for all system operations
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? AdditionalInfo { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    
    // ADHICS Compliance fields
    public string DatabaseSource { get; set; } = string.Empty; // Tracks which database was accessed
    public string ComplianceCategory { get; set; } = string.Empty; // e.g., "DataAccess", "Authentication", "Authorization"
    public string RiskLevel { get; set; } = string.Empty; // "Low", "Medium", "High", "Critical"
}
