using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class SubServiceRepository : ISubServiceRepository
{
    private readonly IDbConnection _connection;

    public SubServiceRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<SubServiceListItemDto>> GetAllAsync(int? serviceId, bool includeInactive)
    {
        var sql = @"
SELECT
    SSId,
    SubServiceTitle,
    ArSubServiceTitle,
    Details,
    ArDetails,
    DisplayOrder,
    Active,
    FId,
    SId
FROM MSubServices
WHERE IsDeleted = 0
  AND (@ServiceId IS NULL OR SId = @ServiceId)
  AND (@IncludeInactive = 1 OR Active = 1)
ORDER BY COALESCE(DisplayOrder, 0) ASC, SSId DESC";

        var rows = await _connection.QueryAsync<SubServiceListItemDto>(sql, new
        {
            ServiceId = serviceId,
            IncludeInactive = includeInactive ? 1 : 0
        });

        return rows.ToList();
    }

    public async Task<List<SubServiceListItemDto>> GetByServiceIdAsync(int serviceId)
    {
        var sql = @"
SELECT
    SSId,
    SubServiceTitle,
    ArSubServiceTitle,
    Details,
    ArDetails,
    DisplayOrder,
    Active,
    FId,
    SId
FROM MSubServices
WHERE IsDeleted = 0
  AND SId = @ServiceId
  AND Active = 1
ORDER BY COALESCE(DisplayOrder, 0) ASC, SSId ASC";

        var rows = await _connection.QueryAsync<SubServiceListItemDto>(sql, new { ServiceId = serviceId });
        return rows.ToList();
    }

    public async Task<MSubService?> GetByIdAsync(int subServiceId)
    {
        var sql = "SELECT * FROM MSubServices WHERE SSId = @SubServiceId AND IsDeleted = 0";
        return await _connection.QueryFirstOrDefaultAsync<MSubService>(sql, new { SubServiceId = subServiceId });
    }

    public async Task<int> UpsertAsync(SubServiceUpsertRequest request)
    {
        if (request.SSId.HasValue)
        {
            var updateSql = @"
UPDATE MSubServices
SET SubServiceTitle = @SubServiceTitle,
    ArSubServiceTitle = @ArSubServiceTitle,
    Details = @Details,
    ArDetails = @ArDetails,
    DisplayOrder = @DisplayOrder,
    Active = @Active,
    FId = @FId,
    SId = @SId
WHERE SSId = @SSId";

            await _connection.ExecuteAsync(updateSql, request);
            return request.SSId.Value;
        }

        var insertSql = @"
INSERT INTO MSubServices
(
    SubServiceTitle, ArSubServiceTitle, Details, ArDetails,
    DisplayOrder, Active, FId, SId, IsDeleted
)
VALUES
(
    @SubServiceTitle, @ArSubServiceTitle, @Details, @ArDetails,
    @DisplayOrder, @Active, @FId, @SId, 0
);
SELECT CAST(SCOPE_IDENTITY() as int);";

        return await _connection.QuerySingleAsync<int>(insertSql, request);
    }

    public async Task<bool> DeleteAsync(int subServiceId)
    {
        var sql = "UPDATE MSubServices SET IsDeleted = 1 WHERE SSId = @SubServiceId";
        var rows = await _connection.ExecuteAsync(sql, new { SubServiceId = subServiceId });
        return rows > 0;
    }
}
