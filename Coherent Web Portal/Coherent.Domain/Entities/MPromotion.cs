namespace Coherent.Domain.Entities;

public class MPromotion
{
    public int PromotionId { get; set; }
    
    // Basic Info
    public string Title { get; set; } = string.Empty;
    public string? ArTitle { get; set; }
    public string? Description { get; set; }
    public string? ArDescription { get; set; }
    
    // Image
    public string ImageFileName { get; set; } = string.Empty;
    
    // Link URL
    public string? LinkUrl { get; set; }
    public string? LinkType { get; set; } // Internal, External, None
    
    // Display settings
    public int DisplayOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    // Status
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
