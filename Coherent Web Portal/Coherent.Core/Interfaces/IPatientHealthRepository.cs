using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

/// <summary>
/// Repository interface for patient health data operations
/// </summary>
public interface IPatientHealthRepository
{
    Task<VitalSignsDto?> GetVitalSignsByMRNOAsync(string mrNo);
    Task<List<MedicationDto>> GetMedicationsByMRNOAsync(string mrNo);
    Task<List<AllergyDto>> GetAllergiesByMRNOAsync(string mrNo);
}
