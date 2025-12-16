using Coherent.Core.DTOs;
using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

public interface ICrmDoctorRepository
{
    Task<List<MDoctor>> GetAllAsync(bool includeInactive);
    Task<MDoctor?> GetByIdAsync(int doctorId);
    Task<int> UpsertAsync(CrmDoctorUpsertRequest request);
}
