using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class CrmDoctorFacilityRepository : ICrmDoctorFacilityRepository
{
    private readonly IDbConnection _connection;

    public CrmDoctorFacilityRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<MDoctorFacility>> GetByDoctorIdAsync(int doctorId)
    {
        var sql = "SELECT * FROM MDoctorFacilities WHERE DId = @DoctorId ORDER BY Id";
        var rows = await _connection.QueryAsync<MDoctorFacility>(sql, new { DoctorId = doctorId });
        return rows.ToList();
    }

    public async Task AddAsync(int doctorId, int facilityId)
    {
        var sql = @"
IF NOT EXISTS (SELECT 1 FROM MDoctorFacilities WHERE DId = @DoctorId AND FId = @FacilityId)
BEGIN
    INSERT INTO MDoctorFacilities (FId, DId) VALUES (@FacilityId, @DoctorId)
END";

        await _connection.ExecuteAsync(sql, new { DoctorId = doctorId, FacilityId = facilityId });
    }

    public async Task RemoveAsync(int doctorId, int facilityId)
    {
        var sql = "DELETE FROM MDoctorFacilities WHERE DId = @DoctorId AND FId = @FacilityId";
        await _connection.ExecuteAsync(sql, new { DoctorId = doctorId, FacilityId = facilityId });
    }
}
