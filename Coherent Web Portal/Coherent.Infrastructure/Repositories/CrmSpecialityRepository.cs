using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class CrmSpecialityRepository : ICrmSpecialityRepository
{
    private readonly IDbConnection _connection;

    public CrmSpecialityRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<MSpeciality>> GetAllAsync(bool includeInactive)
    {
        var sql = includeInactive
            ? "SELECT * FROM MSpecility ORDER BY SpecilityName"
            : "SELECT * FROM MSpecility WHERE Active = 1 ORDER BY SpecilityName";

        var rows = await _connection.QueryAsync<MSpeciality>(sql);
        return rows.ToList();
    }
}
