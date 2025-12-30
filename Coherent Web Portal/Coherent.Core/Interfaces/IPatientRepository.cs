using Coherent.Core.DTOs;
using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

/// <summary>
/// Repository interface for patient operations
/// </summary>
public interface IPatientRepository
{
    Task<(IEnumerable<RegPatient> Patients, int TotalCount)> SearchPatientsAsync(
        string? mrNo,
        string? name,
        string? personSocialSecurityNo,
        string? cellPhone,
        DateTime? visitDateFrom,
        DateTime? visitDateTo,
        bool? onboardedOnMobileApp,
        int pageNumber,
        int pageSize);

    Task<RegPatient?> GetPatientByMRNoAsync(string mrNo);
}
