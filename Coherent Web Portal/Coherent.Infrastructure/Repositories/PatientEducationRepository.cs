using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class PatientEducationRepository : IPatientEducationRepository
{
    private readonly IDbConnection _connection;

    public PatientEducationRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<PatientEducationListItemDto>> GetAllAsync(int? categoryId = null, bool includeInactive = false)
    {
        var sql = @"
SELECT
    e.EducationId,
    e.CategoryId,
    c.CategoryName,
    c.ArCategoryName,
    e.Title,
    e.ArTitle,
    CASE WHEN e.PdfFileName IS NOT NULL AND e.PdfFileName <> '' THEN 1 ELSE 0 END AS HasPdf,
    CASE WHEN e.ContentDeltaJson IS NOT NULL AND e.ContentDeltaJson <> '' THEN 1 ELSE 0 END AS HasContent,
    e.ThumbnailImageName,
    e.Summary,
    e.ArSummary,
    e.DisplayOrder,
    e.Active,
    e.CreatedAt
FROM MPatientEducation e
INNER JOIN MPatientEducationCategory c ON e.CategoryId = c.CategoryId
WHERE e.IsDeleted = 0
  AND (@CategoryId IS NULL OR e.CategoryId = @CategoryId)
  AND (@IncludeInactive = 1 OR e.Active = 1)
ORDER BY COALESCE(e.DisplayOrder, 0) ASC, e.EducationId DESC";

        var rows = await _connection.QueryAsync<PatientEducationListItemDto>(sql, new
        {
            CategoryId = categoryId,
            IncludeInactive = includeInactive ? 1 : 0
        });

        return rows.ToList();
    }

    public async Task<MPatientEducation?> GetByIdAsync(int educationId)
    {
        var sql = "SELECT * FROM MPatientEducation WHERE EducationId = @EducationId AND IsDeleted = 0";
        return await _connection.QueryFirstOrDefaultAsync<MPatientEducation>(sql, new { EducationId = educationId });
    }

    public async Task<PatientEducationDetailDto?> GetDetailByIdAsync(int educationId)
    {
        var sql = @"
SELECT
    e.EducationId,
    e.CategoryId,
    c.CategoryName,
    c.ArCategoryName,
    e.Title,
    e.ArTitle,
    e.ContentDeltaJson,
    e.ArContentDeltaJson,
    e.PdfFileName,
    e.PdfFilePath AS PdfFileUrl,
    e.ThumbnailImageName,
    e.Summary,
    e.ArSummary,
    e.DisplayOrder,
    e.Active,
    e.CreatedAt,
    e.UpdatedAt
FROM MPatientEducation e
INNER JOIN MPatientEducationCategory c ON e.CategoryId = c.CategoryId
WHERE e.EducationId = @EducationId AND e.IsDeleted = 0";

        return await _connection.QueryFirstOrDefaultAsync<PatientEducationDetailDto>(sql, new { EducationId = educationId });
    }

    public async Task<int> UpsertAsync(PatientEducationUpsertRequest request)
    {
        if (request.EducationId.HasValue && request.EducationId.Value > 0)
        {
            var updateSql = @"
UPDATE MPatientEducation
SET CategoryId = @CategoryId,
    Title = @Title,
    ArTitle = @ArTitle,
    ContentDeltaJson = @ContentDeltaJson,
    ArContentDeltaJson = @ArContentDeltaJson,
    Summary = @Summary,
    ArSummary = @ArSummary,
    DisplayOrder = @DisplayOrder,
    Active = COALESCE(@Active, Active),
    UpdatedAt = GETDATE()
WHERE EducationId = @EducationId";

            await _connection.ExecuteAsync(updateSql, request);
            return request.EducationId.Value;
        }
        else
        {
            var insertSql = @"
INSERT INTO MPatientEducation
(
    CategoryId, Title, ArTitle,
    ContentDeltaJson, ArContentDeltaJson,
    Summary, ArSummary,
    DisplayOrder, Active, IsDeleted, CreatedAt
)
VALUES
(
    @CategoryId, @Title, @ArTitle,
    @ContentDeltaJson, @ArContentDeltaJson,
    @Summary, @ArSummary,
    @DisplayOrder, COALESCE(@Active, 1), 0, GETDATE()
);
SELECT CAST(SCOPE_IDENTITY() as int);";

            return await _connection.QuerySingleAsync<int>(insertSql, request);
        }
    }

    public async Task<bool> UpdateThumbnailAsync(int educationId, string thumbnailImageName)
    {
        var sql = @"
UPDATE MPatientEducation
SET ThumbnailImageName = @ThumbnailImageName,
    UpdatedAt = GETDATE()
WHERE EducationId = @EducationId";

        var rows = await _connection.ExecuteAsync(sql, new { EducationId = educationId, ThumbnailImageName = thumbnailImageName });
        return rows > 0;
    }

    public async Task<bool> UpdatePdfAsync(int educationId, string pdfFileName, string pdfFilePath)
    {
        var sql = @"
UPDATE MPatientEducation
SET PdfFileName = @PdfFileName,
    PdfFilePath = @PdfFilePath,
    UpdatedAt = GETDATE()
WHERE EducationId = @EducationId";

        var rows = await _connection.ExecuteAsync(sql, new { EducationId = educationId, PdfFileName = pdfFileName, PdfFilePath = pdfFilePath });
        return rows > 0;
    }

    public async Task<bool> RemovePdfAsync(int educationId)
    {
        var sql = @"
UPDATE MPatientEducation
SET PdfFileName = NULL,
    PdfFilePath = NULL,
    UpdatedAt = GETDATE()
WHERE EducationId = @EducationId";

        var rows = await _connection.ExecuteAsync(sql, new { EducationId = educationId });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int educationId)
    {
        var sql = @"
UPDATE MPatientEducation 
SET IsDeleted = 1, UpdatedAt = GETDATE() 
WHERE EducationId = @EducationId";
        
        var rows = await _connection.ExecuteAsync(sql, new { EducationId = educationId });
        return rows > 0;
    }
}
