namespace Coherent.Domain.Entities;

/// <summary>
/// Represents a third-party system that integrates with the portal
/// </summary>
public class ThirdPartyClient
{
    public Guid Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;
    public string IpWhitelist { get; set; } = string.Empty; // Comma-separated IPs
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastAccessAt { get; set; }
    
    // ADHICS Compliance
    public string ComplianceLevel { get; set; } = string.Empty; // "ADHICS-Level1", "ADHICS-Level2", etc.
    public string SecurityKeyHash { get; set; } = string.Empty;
    public string SecurityKey { get; set; } = string.Empty; // Actual security key for V2 API
    public DateTime SecurityKeyExpiry { get; set; }
    public int MaxRequestsPerMinute { get; set; }
    
    // Permissions
    public string AllowedEndpoints { get; set; } = string.Empty; // JSON array of allowed endpoints
    public string DataAccessLevel { get; set; } = string.Empty; // "Read", "Write", "Full"
}
