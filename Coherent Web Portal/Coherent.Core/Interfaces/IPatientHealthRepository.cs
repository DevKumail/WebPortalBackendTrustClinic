using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

/// <summary>
/// Repository interface for patient health data operations
/// </summary>
public interface IPatientHealthRepository
{
    Task<List<VitalSignsDto>> GetVitalSignsByMRNOAsync(string mrNo, int limit = 50);
    Task<List<MedicationDto>> GetMedicationsByMRNOAsync(string mrNo);
    Task<List<MedicationV2Dto>> GetMedicationsV2ByMRNOAsync(string mrNo);
    Task<List<AllergyDto>> GetAllergiesByMRNOAsync(string mrNo);
    Task<List<DiagnosisDto>> GetDiagnosisByMRNOAsync(string mrNo);
}
