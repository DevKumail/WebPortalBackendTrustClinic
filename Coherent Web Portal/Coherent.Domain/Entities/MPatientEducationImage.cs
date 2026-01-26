namespace Coherent.Domain.Entities;

public class MPatientEducationImage
{
    public int ImageId { get; set; }
    public int EducationId { get; set; }
    public string? ImageFileName { get; set; }
    public string? ImageCaption { get; set; }
    public string? ArImageCaption { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}
