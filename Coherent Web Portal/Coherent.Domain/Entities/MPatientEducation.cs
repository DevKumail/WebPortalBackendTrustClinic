namespace Coherent.Domain.Entities;

public class MPatientEducation
{
    public int EducationId { get; set; }
    public int CategoryId { get; set; }
    public string? Title { get; set; }
    public string? ArTitle { get; set; }
    
    // Rich Text Content (Quill Delta JSON format - includes inline images)
    public string? ContentDeltaJson { get; set; }
    public string? ArContentDeltaJson { get; set; }
    
    // PDF Document (separate, downloadable)
    public string? PdfFileName { get; set; }
    public string? PdfFilePath { get; set; }
    
    // Thumbnail
    public string? ThumbnailImageName { get; set; }
    
    // Summary
    public string? Summary { get; set; }
    public string? ArSummary { get; set; }
    
    // Display Settings
    public int? DisplayOrder { get; set; }
    public bool Active { get; set; }
    public bool IsDeleted { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}
