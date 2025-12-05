namespace Coherent.Domain.Entities;

/// <summary>
/// Doctor entity from CoherentMobApp database
/// </summary>
public class MDoctor
{
    public int DId { get; set; }
    public string? DoctorName { get; set; }
    public string? ArDoctorName { get; set; }
    public string? Title { get; set; }
    public string? ArTitle { get; set; }
    public int? SPId { get; set; }
    public string? YearsOfExperience { get; set; }
    public string? Nationality { get; set; }
    public string? ArNationality { get; set; }
    public string? Languages { get; set; }
    public string? ArLanguages { get; set; }
    public string? DoctorPhotoName { get; set; }
    public string? About { get; set; }
    public string? ArAbout { get; set; }
    public string? Education { get; set; }
    public string? ArEducation { get; set; }
    public string? Experience { get; set; }
    public string? ArExperience { get; set; }
    public string? Expertise { get; set; }
    public string? ArExpertise { get; set; }
    public string? LicenceNo { get; set; }
    public bool? Active { get; set; }
    public string? Gender { get; set; }
}
