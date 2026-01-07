namespace Coherent.Domain.Entities;

public class MSubService
{
    public int SSId { get; set; }
    public string? SubServiceTitle { get; set; }
    public string? ArSubServiceTitle { get; set; }
    public string? Details { get; set; }
    public string? ArDetails { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? Active { get; set; }
    public int? FId { get; set; }
    public int? SId { get; set; }
    public bool IsDeleted { get; set; }
}
