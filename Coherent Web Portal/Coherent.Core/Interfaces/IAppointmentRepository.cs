using Coherent.Core.DTOs;
using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

/// <summary>
/// Repository interface for appointment operations
/// </summary>
public interface IAppointmentRepository
{
    // Appointment Management
    Task<List<AppointmentDto>> GetAllAppointmentsByMRNOAsync(string mrNo);
    Task<long> BookAppointmentAsync(BookAppointmentRequest request);
    Task<bool> ModifyAppointmentAsync(ModifyAppointmentRequest request);
    Task<SchAppointment?> GetAppointmentByIdAsync(long appId);
    
    // Available Slots
    Task<List<DoctorSlotsDto>> GetAvailableSlotsOfDoctorAsync(GetAvailableSlotsRequest request);
    
    // Doctor Management
    Task<List<DoctorProfileDto>> GetAllDoctorsAsync();
    Task<DoctorProfileDto?> GetDoctorByIdAsync(int doctorId);
    
    // Helper methods for slot calculation
    Task<List<ProviderSchedule>> GetProviderSchedulesAsync(long providerId, DateTime fromDate, DateTime toDate);
    Task<List<HolidaySchedule>> GetHolidaysAsync(int year, string monthDay, long? siteId);
    Task<List<SchBlockTimeslot>> GetBlockedTimeslotsAsync(long providerId, string date, long? siteId);
    Task<List<SchAppointment>> GetExistingAppointmentsAsync(long providerId, string date, long siteId);
}
