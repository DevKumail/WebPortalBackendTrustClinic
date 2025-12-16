using Coherent.Core.DTOs;
using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

public interface ICrmFacilityRepository
{
    Task<List<MFacility>> GetAllAsync(bool includeInactive);
    Task<MFacility?> GetByIdAsync(int facilityId);
    Task<int> UpsertAsync(CrmFacilityUpsertRequest request);
}
