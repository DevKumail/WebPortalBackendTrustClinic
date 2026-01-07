using Coherent.Core.DTOs;
using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

public interface ISubServiceRepository
{
    Task<List<SubServiceListItemDto>> GetAllAsync(int? serviceId, bool includeInactive);
    Task<List<SubServiceListItemDto>> GetByServiceIdAsync(int serviceId);
    Task<MSubService?> GetByIdAsync(int subServiceId);
    Task<int> UpsertAsync(SubServiceUpsertRequest request);
    Task<bool> DeleteAsync(int subServiceId);
}
