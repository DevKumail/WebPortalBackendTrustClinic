using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class PatientEducationCategoryRepository : IPatientEducationCategoryRepository
{
    private readonly IDbConnection _connection;

    public PatientEducationCategoryRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<PatientEducationCategoryListItemDto>> GetAllAsync(bool includeInactive = false)
    {
        var sql = @"
SELECT
    c.CategoryId,
    c.CategoryName,
    c.ArCategoryName,
    c.CategoryDescription,
    c.ArCategoryDescription,
    c.IconImageName,
    c.DisplayOrder,
    c.IsGeneral,
    c.Active,
    (SELECT COUNT(*) FROM MPatientEducation e WHERE e.CategoryId = c.CategoryId AND e.IsDeleted = 0) AS EducationCount
FROM MPatientEducationCategory c
WHERE c.IsDeleted = 0
  AND (@IncludeInactive = 1 OR c.Active = 1)
ORDER BY COALESCE(c.DisplayOrder, 0) ASC, c.CategoryId DESC";

        var rows = await _connection.QueryAsync<PatientEducationCategoryListItemDto>(sql, new
        {
            IncludeInactive = includeInactive ? 1 : 0
        });

        return rows.ToList();
    }

    public async Task<List<PatientEducationCategoryDropdownDto>> GetDropdownListAsync()
    {
        var sql = @"
SELECT
    CategoryId,
    CategoryName,
    ArCategoryName,
    IsGeneral
FROM MPatientEducationCategory
WHERE IsDeleted = 0 AND Active = 1
ORDER BY COALESCE(DisplayOrder, 0) ASC, CategoryName ASC";

        var rows = await _connection.QueryAsync<PatientEducationCategoryDropdownDto>(sql);
        return rows.ToList();
    }

    public async Task<MPatientEducationCategory?> GetByIdAsync(int categoryId)
    {
        var sql = "SELECT * FROM MPatientEducationCategory WHERE CategoryId = @CategoryId AND IsDeleted = 0";
        return await _connection.QueryFirstOrDefaultAsync<MPatientEducationCategory>(sql, new { CategoryId = categoryId });
    }

    public async Task<int> UpsertAsync(PatientEducationCategoryUpsertRequest request)
    {
        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            var updateSql = @"
UPDATE MPatientEducationCategory
SET CategoryName = @CategoryName,
    ArCategoryName = @ArCategoryName,
    CategoryDescription = @CategoryDescription,
    ArCategoryDescription = @ArCategoryDescription,
    IconImageName = COALESCE(@IconImageName, IconImageName),
    DisplayOrder = @DisplayOrder,
    IsGeneral = COALESCE(@IsGeneral, IsGeneral),
    Active = COALESCE(@Active, Active),
    UpdatedAt = GETDATE()
WHERE CategoryId = @CategoryId";

            await _connection.ExecuteAsync(updateSql, request);
            return request.CategoryId.Value;
        }
        else
        {
            var insertSql = @"
INSERT INTO MPatientEducationCategory
(
    CategoryName, ArCategoryName, CategoryDescription, ArCategoryDescription,
    IconImageName, DisplayOrder, IsGeneral, Active, IsDeleted, CreatedAt
)
VALUES
(
    @CategoryName, @ArCategoryName, @CategoryDescription, @ArCategoryDescription,
    @IconImageName, @DisplayOrder, COALESCE(@IsGeneral, 0), COALESCE(@Active, 1), 0, GETDATE()
);
SELECT CAST(SCOPE_IDENTITY() as int);";

            return await _connection.QuerySingleAsync<int>(insertSql, request);
        }
    }

    public async Task<bool> UpdateIconImageAsync(int categoryId, string iconImageName)
    {
        var sql = @"
UPDATE MPatientEducationCategory
SET IconImageName = @IconImageName,
    UpdatedAt = GETDATE()
WHERE CategoryId = @CategoryId";

        var rows = await _connection.ExecuteAsync(sql, new { CategoryId = categoryId, IconImageName = iconImageName });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int categoryId)
    {
        var sql = @"
UPDATE MPatientEducationCategory 
SET IsDeleted = 1, UpdatedAt = GETDATE() 
WHERE CategoryId = @CategoryId";
        
        var rows = await _connection.ExecuteAsync(sql, new { CategoryId = categoryId });
        return rows > 0;
    }
}
