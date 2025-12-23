using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

public interface ICrmSpecialityRepository
{
    Task<List<MSpeciality>> GetAllAsync(bool includeInactive);
}
