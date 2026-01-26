using Coherent.Core.DTOs;
using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

public interface IPatientEducationCategoryRepository
{
    Task<List<PatientEducationCategoryListItemDto>> GetAllAsync(bool includeInactive = false);
    Task<List<PatientEducationCategoryDropdownDto>> GetDropdownListAsync();
    Task<MPatientEducationCategory?> GetByIdAsync(int categoryId);
    Task<int> UpsertAsync(PatientEducationCategoryUpsertRequest request);
    Task<bool> UpdateIconImageAsync(int categoryId, string iconImageName);
    Task<bool> DeleteAsync(int categoryId);
}
