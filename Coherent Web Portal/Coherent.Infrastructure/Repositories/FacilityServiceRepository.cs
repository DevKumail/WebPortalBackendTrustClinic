using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Dapper;
using System.Data;
using System.Linq;

namespace Coherent.Infrastructure.Repositories;

public class FacilityServiceRepository : IFacilityServiceRepository
{
    private readonly IDbConnection _connection;

    public FacilityServiceRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<FacilityServiceListItemDto>> GetAllAsync(int? facilityId, bool includeInactive)
    {
        var sql = @"
SELECT
    s.SId,
    s.FId,
    s.ServiceTitle,
    s.ArServiceTitle,
    s.ServiceIntro,
    s.ArServiceIntro,
    s.Active,
    s.DisplayOrder,
    s.DisplayImageName,
    s.IconImageName
FROM MServices s
WHERE (@FacilityId IS NULL OR s.FId = @FacilityId)
  AND (@IncludeInactive = 1 OR s.Active = 1)
ORDER BY COALESCE(s.DisplayOrder, 0) ASC, s.SId DESC";

        var rows = await _connection.QueryAsync<FacilityServiceListItemDto>(sql, new
        {
            FacilityId = facilityId,
            IncludeInactive = includeInactive ? 1 : 0
        });

        return rows.ToList();
    }

    public async Task<MService?> GetByIdAsync(int serviceId)
    {
        var sql = "SELECT * FROM MServices WHERE SId = @ServiceId";
        return await _connection.QueryFirstOrDefaultAsync<MService>(sql, new { ServiceId = serviceId });
    }

    public async Task<int> UpsertAsync(FacilityServiceUpsertRequest request)
    {
        if (request.SId.HasValue)
        {
            var updateSql = @"
UPDATE MServices
SET FId = @FId,
    ServiceTitle = @ServiceTitle,
    ArServiceTitle = @ArServiceTitle,
    ServiceIntro = @ServiceIntro,
    ArServiceIntro = @ArServiceIntro,
    Active = @Active,
    DisplayOrder = @DisplayOrder,
    DisplayImageName = @DisplayImageName,
    IconImageName = @IconImageName
WHERE SId = @SId";

            await _connection.ExecuteAsync(updateSql, request);
            return request.SId.Value;
        }

        var insertSql = @"
INSERT INTO MServices
(
    FId, ServiceTitle, ArServiceTitle, ServiceIntro, ArServiceIntro,
    Active, DisplayOrder, DisplayImageName, IconImageName
)
VALUES
(
    @FId, @ServiceTitle, @ArServiceTitle, @ServiceIntro, @ArServiceIntro,
    @Active, @DisplayOrder, @DisplayImageName, @IconImageName
);
SELECT CAST(SCOPE_IDENTITY() as int);";

        return await _connection.QuerySingleAsync<int>(insertSql, request);
    }

    public async Task<bool> UpdateDisplayImageAsync(int serviceId, string displayImageName)
    {
        var sql = @"
UPDATE MServices
SET DisplayImageName = @DisplayImageName
WHERE SId = @ServiceId";

        var rows = await _connection.ExecuteAsync(sql, new { ServiceId = serviceId, DisplayImageName = displayImageName });
        return rows > 0;
    }

    public async Task<bool> UpdateIconImageAsync(int serviceId, string iconImageName)
    {
        var sql = @"
UPDATE MServices
SET IconImageName = @IconImageName
WHERE SId = @ServiceId";

        var rows = await _connection.ExecuteAsync(sql, new { ServiceId = serviceId, IconImageName = iconImageName });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int serviceId)
    {
        var sql = "DELETE FROM MServices WHERE SId = @ServiceId";
        var rows = await _connection.ExecuteAsync(sql, new { ServiceId = serviceId });
        return rows > 0;
    }
}
