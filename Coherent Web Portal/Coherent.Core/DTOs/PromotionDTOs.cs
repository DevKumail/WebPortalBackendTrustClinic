namespace Coherent.Core.DTOs;

#region Promotion List/Detail DTOs

public class PromotionListDto
{
    public int PromotionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ArTitle { get; set; }
    public string? ImageFileName { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkType { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class PromotionDetailDto : PromotionListDto
{
    public string? Description { get; set; }
    public string? ArDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

#endregion

#region Promotion Request DTOs

public class PromotionUpsertRequest
{
    public int? PromotionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ArTitle { get; set; }
    public string? Description { get; set; }
    public string? ArDescription { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkType { get; set; } // Internal, External, None
    public int DisplayOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

#endregion

#region Mobile App DTOs

public class PromotionSliderDto
{
    public int PromotionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ArTitle { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkType { get; set; }
    public int DisplayOrder { get; set; }
}

#endregion
