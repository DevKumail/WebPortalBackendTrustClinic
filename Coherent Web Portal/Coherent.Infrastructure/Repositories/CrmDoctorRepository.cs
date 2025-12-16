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

    public async Task<List<MDoctor>> GetAllAsync(bool includeInactive)
    {
        var sql = includeInactive
            ? "SELECT * FROM MDoctors ORDER BY DoctorName"
            : "SELECT * FROM MDoctors WHERE Active = 1 ORDER BY DoctorName";

        var rows = await _connection.QueryAsync<MDoctor>(sql);
        return rows.ToList();
    }

    public async Task<MDoctor?> GetByIdAsync(int doctorId)
    {
        var sql = "SELECT * FROM MDoctors WHERE DId = @DoctorId";
        return await _connection.QueryFirstOrDefaultAsync<MDoctor>(sql, new { DoctorId = doctorId });
    }

    public async Task<int> UpsertAsync(CrmDoctorUpsertRequest request)
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
}
