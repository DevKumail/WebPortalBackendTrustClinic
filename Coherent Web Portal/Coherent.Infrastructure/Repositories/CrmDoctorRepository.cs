using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class CrmDoctorRepository : ICrmDoctorRepository
{
    private readonly IDbConnection _connection;

    public CrmDoctorRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<CrmDoctorListItemDto>> GetAllAsync(bool includeInactive)
    {
        var sql = @"
SELECT
    d.DId,
    d.DoctorName,
    d.ArDoctorName,
    d.Title,
    d.ArTitle,
    d.SPId,
    s.SpecilityName AS SpecilityName,
    s.ArSpecilityName AS ArSpecilityName,
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
    d.Active,
    d.Gender
FROM MDoctors d
LEFT JOIN MSpecility s ON d.SPId = s.SPId
WHERE (@IncludeInactive = 1 OR d.Active = 1)
ORDER BY d.DoctorName";

        var rows = await _connection.QueryAsync<CrmDoctorListItemDto>(sql, new
        {
            IncludeInactive = includeInactive ? 1 : 0
        });

        return rows.ToList();
    }

    public async Task<MDoctor?> GetByIdAsync(int doctorId)
    {
        var sql = "SELECT * FROM MDoctors WHERE DId = @DoctorId";
        return await _connection.QueryFirstOrDefaultAsync<MDoctor>(sql, new { DoctorId = doctorId });
    }

    public async Task<int> UpsertAsync(CrmDoctorUpsertRequest request)
    {
        try
        {

       
        if (request.DId.HasValue)
        {
            var updateSql = @"
UPDATE MDoctors
SET DoctorName = @DoctorName,
    ArDoctorName = @ArDoctorName,
    Title = @Title,
    ArTitle = @ArTitle,
    SPId = @SPId,
    YearsOfExperience = @YearsOfExperience,
    Nationality = @Nationality,
    ArNationality = @ArNationality,
    Languages = @Languages,
    ArLanguages = @ArLanguages,
    DoctorPhotoName = @DoctorPhotoName,
    About = @About,
    ArAbout = @ArAbout,
    Education = @Education,
    ArEducation = @ArEducation,
    Experience = @Experience,
    ArExperience = @ArExperience,
    Expertise = @Expertise,
    ArExpertise = @ArExpertise,
    LicenceNo = @LicenceNo,
    Active = @Active,
    Gender = @Gender
WHERE DId = @DId";

            await _connection.ExecuteAsync(updateSql, request);
            return request.DId.Value;
        }

        var insertSql = @"
INSERT INTO MDoctors
(
    DoctorName, ArDoctorName, Title, ArTitle, SPId, YearsOfExperience,
    Nationality, ArNationality, Languages, ArLanguages, DoctorPhotoName,
    About, ArAbout, Education, ArEducation, Experience, ArExperience,
    Expertise, ArExpertise, LicenceNo, Active, Gender
)
VALUES
(
    @DoctorName, @ArDoctorName, @Title, @ArTitle, @SPId, @YearsOfExperience,
    @Nationality, @ArNationality, @Languages, @ArLanguages, @DoctorPhotoName,
    @About, @ArAbout, @Education, @ArEducation, @Experience, @ArExperience,
    @Expertise, @ArExpertise, @LicenceNo, @Active, @Gender
);
SELECT CAST(SCOPE_IDENTITY() as int);";

        var newId = await _connection.QuerySingleAsync<int>(insertSql, request);
        return newId;
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    public async Task<bool> UpdateDoctorPhotoAsync(int doctorId, string doctorPhotoName)
    {
        var sql = @"
UPDATE MDoctors
SET DoctorPhotoName = @DoctorPhotoName
WHERE DId = @DoctorId";

        var rows = await _connection.ExecuteAsync(sql, new
        {
            DoctorId = doctorId,
            DoctorPhotoName = doctorPhotoName
        });

        return rows > 0;
    }
}
