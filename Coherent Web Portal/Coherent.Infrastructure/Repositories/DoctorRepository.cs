using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

/// <summary>
/// Repository for doctor operations from CoherentMobApp database
/// </summary>
public class DoctorRepository : IDoctorRepository
{
    private readonly IDbConnection _connection;

    public DoctorRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<DoctorProfileDto>> GetAllDoctorsAsync()
    {
        var query = @"
            SELECT 
                d.DId,
                d.DoctorName,
                d.ArDoctorName,
                d.Title,
                d.ArTitle,
                s.SpecilityName AS Speciality,
                s.ArSpecilityName AS ArSpeciality,
                d.YearsOfExperience,
                d.Nationality,
                d.ArNationality,
                d.Languages,
                d.ArLanguages,
                d.DoctorPhotoName,
                d.About,
                d.ArAbout,
                d.Education,
                d.ArEducation,
                d.Experience,
                d.ArExperience,
                d.Expertise,
                d.ArExpertise,
                d.LicenceNo,
                d.Gender,
                d.Active
            FROM MDoctors d
            LEFT JOIN MSpecility s ON d.SPId = s.SPId
            WHERE d.Active = 1
            ORDER BY d.DoctorName";

        var doctors = await _connection.QueryAsync<DoctorProfileDto>(query);

        // Get facilities for each doctor
        var doctorsList = doctors.ToList();
        foreach (var doctor in doctorsList)
        {
            var facilitiesQuery = @"
                SELECT f.FName
                FROM MDoctorFacilities df
                INNER JOIN MFacility f ON df.FId = f.FId
                WHERE df.DId = @DoctorId";

            var facilities = await _connection.QueryAsync<string>(facilitiesQuery, new { DoctorId = doctor.DId });
            doctor.Facilities = facilities.ToList();
        }

        return doctorsList;
    }

    public async Task<DoctorProfileDto?> GetDoctorByIdAsync(int doctorId)
    {
        var query = @"
            SELECT 
                d.DId,
                d.DoctorName,
                d.ArDoctorName,
                d.Title,
                d.ArTitle,
                s.SpecilityName AS Speciality,
                s.ArSpecilityName AS ArSpeciality,
                d.YearsOfExperience,
                d.Nationality,
                d.ArNationality,
                d.Languages,
                d.ArLanguages,
                d.DoctorPhotoName,
                d.About,
                d.ArAbout,
                d.Education,
                d.ArEducation,
                d.Experience,
                d.ArExperience,
                d.Expertise,
                d.ArExpertise,
                d.LicenceNo,
                d.Gender,
                d.Active
            FROM MDoctors d
            LEFT JOIN MSpecility s ON d.SPId = s.SPId
            WHERE d.DId = @DoctorId";

        var doctor = await _connection.QueryFirstOrDefaultAsync<DoctorProfileDto>(query, new { DoctorId = doctorId });

        if (doctor != null)
        {
            // Get facilities
            var facilitiesQuery = @"
                SELECT f.FName
                FROM MDoctorFacilities df
                INNER JOIN MFacility f ON df.FId = f.FId
                WHERE df.DId = @DoctorId";

            var facilities = await _connection.QueryAsync<string>(facilitiesQuery, new { DoctorId = doctorId });
            doctor.Facilities = facilities.ToList();
        }

        return doctor;
    }
}
