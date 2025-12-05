using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

/// <summary>
/// Repository interface for doctor operations
/// </summary>
public interface IDoctorRepository
{
    Task<List<DoctorProfileDto>> GetAllDoctorsAsync();
    Task<DoctorProfileDto?> GetDoctorByIdAsync(int doctorId);
}
