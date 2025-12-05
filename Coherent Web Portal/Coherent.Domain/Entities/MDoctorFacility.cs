namespace Coherent.Domain.Entities;

/// <summary>
/// Doctor-Facility mapping entity from CoherentMobApp database
/// </summary>
public class MDoctorFacility
{
    public int Id { get; set; }
    public int? FId { get; set; }
    public int? DId { get; set; }
}
