using System.Text.Json.Serialization;

namespace Coherent.Core.DTOs;

public class CrmDoctorUpsertRequest
{
    public int? DId { get; set; }

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

public class CrmFacilityUpsertRequest
{
    public int? FId { get; set; }

    public string? FName { get; set; }
    public string? LicenceNo { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? EmailAddress { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? FbUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? TiktokUrl { get; set; }
    public string? Instagram { get; set; }
    public string? WhatsappNo { get; set; }
    public string? GoogleMapUrl { get; set; }
    public string? About { get; set; }
    public string? AboutShort { get; set; }
    public string? FacilityImages { get; set; }
    public string? ArAbout { get; set; }
    public string? ArAboutShort { get; set; }
}

public class CrmDoctorFacilityUpsertRequest
{
    [JsonPropertyName("doctorId")]
    public int DoctorId { get; set; }

    [JsonPropertyName("facilityId")]
    public int FacilityId { get; set; }
}
