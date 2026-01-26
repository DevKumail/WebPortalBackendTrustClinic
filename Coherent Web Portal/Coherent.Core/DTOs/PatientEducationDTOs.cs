using System.Text.Json.Serialization;

namespace Coherent.Core.DTOs;

#region Category DTOs

public class PatientEducationCategoryUpsertRequest
{
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ArCategoryName { get; set; }
    public string? CategoryDescription { get; set; }
    public string? ArCategoryDescription { get; set; }
    public string? IconImageName { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsGeneral { get; set; }
    public bool? Active { get; set; }
}

public class PatientEducationCategoryListItemDto
{
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ArCategoryName { get; set; }
    public string? CategoryDescription { get; set; }
    public string? ArCategoryDescription { get; set; }
    public string? IconImageName { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsGeneral { get; set; }
    public bool? Active { get; set; }
    public int EducationCount { get; set; }
}

public class PatientEducationCategoryDropdownDto
{
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ArCategoryName { get; set; }
    public bool IsGeneral { get; set; }
}

#endregion

#region Education Content DTOs

public class PatientEducationUpsertRequest
{
    public int? EducationId { get; set; }
    public int CategoryId { get; set; }
    public string? Title { get; set; }
    public string? ArTitle { get; set; }
    
    // Rich Text Content (Quill Delta JSON format - includes inline images)
    public string? ContentDeltaJson { get; set; }
    public string? ArContentDeltaJson { get; set; }
    
    // Summary
    public string? Summary { get; set; }
    public string? ArSummary { get; set; }
    
    // Display Settings
    public int? DisplayOrder { get; set; }
    public bool? Active { get; set; }
}

public class PatientEducationListItemDto
{
    public int EducationId { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ArCategoryName { get; set; }
    public string? Title { get; set; }
    public string? ArTitle { get; set; }
    public bool HasPdf { get; set; }
    public bool HasContent { get; set; }
    public string? ThumbnailImageName { get; set; }
    public string? Summary { get; set; }
    public string? ArSummary { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? Active { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PatientEducationDetailDto
{
    public int EducationId { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ArCategoryName { get; set; }
    public string? Title { get; set; }
    public string? ArTitle { get; set; }
    
    // Rich Text Content (Quill Delta JSON format - includes inline images)
    public string? ContentDeltaJson { get; set; }
    public string? ArContentDeltaJson { get; set; }
    
    // PDF Document (separate, downloadable)
    public string? PdfFileName { get; set; }
    public string? PdfFileUrl { get; set; }
    
    // Thumbnail
    public string? ThumbnailImageName { get; set; }
    public string? ThumbnailImageUrl { get; set; }
    
    // Summary
    public string? Summary { get; set; }
    public string? ArSummary { get; set; }
    
    // Display Settings
    public int? DisplayOrder { get; set; }
    public bool? Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

#endregion

#region Patient Education Assignment DTOs

public class PatientEducationAssignmentRequest
{
    public string MRNo { get; set; } = string.Empty;
    public int EducationId { get; set; }
    public string? Notes { get; set; }
    public string? ArNotes { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class PatientEducationBulkAssignRequest
{
    public List<string> MRNos { get; set; } = new();
    public int EducationId { get; set; }
    public string? Notes { get; set; }
    public string? ArNotes { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class PatientEducationAssignmentListDto
{
    public int AssignmentId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientMRN { get; set; }
    public int EducationId { get; set; }
    public string? EducationTitle { get; set; }
    public string? ArEducationTitle { get; set; }
    public string? CategoryName { get; set; }
    public int? AssignedByUserId { get; set; }
    public string? AssignedByUserName { get; set; }
    public DateTime AssignedAt { get; set; }
    public string? Notes { get; set; }
    public string? ArNotes { get; set; }
    public bool IsViewed { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}

public class PatientEducationAssignmentDetailDto : PatientEducationAssignmentListDto
{
    public PatientEducationDetailDto? Education { get; set; }
}

#endregion
