using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

public interface ICrmDoctorFacilityRepository
{
    Task<List<MDoctorFacility>> GetByDoctorIdAsync(int doctorId);
    Task AddAsync(int doctorId, int facilityId);
    Task RemoveAsync(int doctorId, int facilityId);
}
