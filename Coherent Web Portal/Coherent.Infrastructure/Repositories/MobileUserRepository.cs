using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class MobileUserRepository : IMobileUserRepository
{
    private readonly IDbConnection _connection;

    public MobileUserRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<(IEnumerable<MobileUserListItemDto> Users, int TotalCount)> SearchMobileUsersAsync(
        string? mrNo,
        int pageNumber,
        int pageSize)
    {
        var sanitizedMrNo = (mrNo ?? string.Empty).Trim();

        // Determine which "active" column exists (if any) to enforce mobile-active.
        // This avoids runtime failures if the column differs across environments.
        var activeColumn = await DetectActiveColumnAsync();

        var whereClauses = new List<string> { "u.IsDeleted = 0" };
        if (!string.IsNullOrWhiteSpace(activeColumn))
        {
            whereClauses.Add($"u.[{activeColumn}] = 1");
        }

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(sanitizedMrNo))
        {
            parameters.Add("MRNo", $"%{sanitizedMrNo}%");
            whereClauses.Add("u.MRNO LIKE @MRNo");
        }

        var whereClause = "WHERE " + string.Join(" AND ", whereClauses);

        // Total count
        var countSql = $@"
SELECT COUNT(1)
FROM dbo.Users u
{whereClause};";

        var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

        // Pagination
        var offset = (pageNumber - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var dataSql = $@"
SELECT
    u.Id,
    u.MRNO AS MRNo,
    u.FullName
FROM dbo.Users u
{whereClause}
ORDER BY u.Id DESC
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;";

        var users = await _connection.QueryAsync<MobileUserListItemDto>(dataSql, parameters);
        return (users, totalCount);
    }

    private async Task<string?> DetectActiveColumnAsync()
    {
        // Try a few common names used for "active" in the mobile Users table.
        var candidates = new[] { "IsActive", "Active", "IsMobileActive", "MobileActive", "IsEnabled", "Enabled" };

        const string sql = @"
SELECT c.name
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'dbo'
  AND t.name = 'Users'
  AND c.name = @ColumnName;";

        foreach (var col in candidates)
        {
            var found = await _connection.QueryFirstOrDefaultAsync<string?>(sql, new { ColumnName = col });
            if (!string.IsNullOrWhiteSpace(found))
                return found;
        }

        return null;
    }
}
