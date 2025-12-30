using Coherent.Core.DTOs;
using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

public interface IFacilityServiceRepository
{
    Task<List<FacilityServiceListItemDto>> GetAllAsync(int? facilityId, bool includeInactive);
    Task<MService?> GetByIdAsync(int serviceId);
    Task<int> UpsertAsync(FacilityServiceUpsertRequest request);
    Task<bool> UpdateDisplayImageAsync(int serviceId, string displayImageName);
    Task<bool> UpdateIconImageAsync(int serviceId, string iconImageName);

    Task<bool> DeleteAsync(int serviceId);
}
