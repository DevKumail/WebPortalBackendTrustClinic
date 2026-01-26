using Coherent.Core.DTOs;
using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

public interface IPatientEducationRepository
{
    Task<List<PatientEducationListItemDto>> GetAllAsync(int? categoryId = null, bool includeInactive = false);
    Task<MPatientEducation?> GetByIdAsync(int educationId);
    Task<PatientEducationDetailDto?> GetDetailByIdAsync(int educationId);
    Task<int> UpsertAsync(PatientEducationUpsertRequest request);
    Task<bool> UpdateThumbnailAsync(int educationId, string thumbnailImageName);
    Task<bool> UpdatePdfAsync(int educationId, string pdfFileName, string pdfFilePath);
    Task<bool> RemovePdfAsync(int educationId);
    Task<bool> DeleteAsync(int educationId);
}
