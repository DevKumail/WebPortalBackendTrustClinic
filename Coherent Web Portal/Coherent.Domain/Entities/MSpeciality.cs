namespace Coherent.Domain.Entities;

/// <summary>
/// Medical speciality entity from CoherentMobApp database
/// </summary>
public class MSpeciality
{
    public int SPId { get; set; }
    public string? SpecilityName { get; set; }
    public string? ArSpecilityName { get; set; }
    public bool? Active { get; set; }
    public int? FId { get; set; }
}
