namespace Coherent.Domain.Entities;

public class MPatientEducationCategory
{
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ArCategoryName { get; set; }
    public string? CategoryDescription { get; set; }
    public string? ArCategoryDescription { get; set; }
    public string? IconImageName { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsGeneral { get; set; }
    public bool Active { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}
