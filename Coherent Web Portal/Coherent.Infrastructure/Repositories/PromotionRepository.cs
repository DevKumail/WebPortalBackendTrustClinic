using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class PromotionRepository : IPromotionRepository
{
    private readonly IDbConnection _connection;

    public PromotionRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<PromotionListDto>> GetAllAsync(bool? isActive = null)
    {
        var sql = @"
SELECT 
    PromotionId,
    Title,
    ArTitle,
    ImageFileName,
    LinkUrl,
    LinkType,
    DisplayOrder,
    StartDate,
    EndDate,
    IsActive
FROM MPromotion
WHERE IsDeleted = 0
    AND (@IsActive IS NULL OR IsActive = @IsActive)
ORDER BY DisplayOrder, CreatedAt DESC";

        var rows = await _connection.QueryAsync<PromotionListDto>(sql, new { IsActive = isActive });
        return rows.ToList();
    }

    public async Task<List<PromotionSliderDto>> GetActiveForMobileAsync()
    {
        var sql = @"
SELECT 
    PromotionId,
    Title,
    ArTitle,
    ImageFileName AS ImageUrl,
    LinkUrl,
    LinkType,
    DisplayOrder
FROM MPromotion
WHERE IsDeleted = 0
    AND IsActive = 1
    AND (StartDate IS NULL OR StartDate <= GETDATE())
    AND (EndDate IS NULL OR EndDate >= GETDATE())
ORDER BY DisplayOrder, CreatedAt DESC";

        var rows = await _connection.QueryAsync<PromotionSliderDto>(sql);
        return rows.ToList();
    }

    public async Task<PromotionDetailDto?> GetByIdAsync(int promotionId)
    {
        var sql = @"
SELECT 
    PromotionId,
    Title,
    ArTitle,
    Description,
    ArDescription,
    ImageFileName,
    LinkUrl,
    LinkType,
    DisplayOrder,
    StartDate,
    EndDate,
    IsActive,
    CreatedAt,
    CreatedBy,
    UpdatedAt,
    UpdatedBy
FROM MPromotion
WHERE PromotionId = @PromotionId AND IsDeleted = 0";

        return await _connection.QueryFirstOrDefaultAsync<PromotionDetailDto>(sql, new { PromotionId = promotionId });
    }

    public async Task<int> UpsertAsync(PromotionUpsertRequest request, int? userId)
    {
        if (request.PromotionId.HasValue && request.PromotionId.Value > 0)
        {
            // Update
            var updateSql = @"
UPDATE MPromotion
SET Title = @Title,
    ArTitle = @ArTitle,
    Description = @Description,
    ArDescription = @ArDescription,
    LinkUrl = @LinkUrl,
    LinkType = @LinkType,
    DisplayOrder = @DisplayOrder,
    StartDate = @StartDate,
    EndDate = @EndDate,
    IsActive = @IsActive,
    UpdatedAt = GETDATE(),
    UpdatedBy = @UserId
WHERE PromotionId = @PromotionId AND IsDeleted = 0";

            await _connection.ExecuteAsync(updateSql, new
            {
                request.PromotionId,
                request.Title,
                request.ArTitle,
                request.Description,
                request.ArDescription,
                request.LinkUrl,
                request.LinkType,
                request.DisplayOrder,
                request.StartDate,
                request.EndDate,
                request.IsActive,
                UserId = userId
            });

            return request.PromotionId.Value;
        }
        else
        {
            // Insert
            var insertSql = @"
INSERT INTO MPromotion
(Title, ArTitle, Description, ArDescription, ImageFileName, LinkUrl, LinkType, DisplayOrder, StartDate, EndDate, IsActive, IsDeleted, CreatedAt, CreatedBy)
VALUES
(@Title, @ArTitle, @Description, @ArDescription, '', @LinkUrl, @LinkType, @DisplayOrder, @StartDate, @EndDate, @IsActive, 0, GETDATE(), @UserId);
SELECT CAST(SCOPE_IDENTITY() as int);";

            return await _connection.QuerySingleAsync<int>(insertSql, new
            {
                request.Title,
                request.ArTitle,
                request.Description,
                request.ArDescription,
                request.LinkUrl,
                request.LinkType,
                request.DisplayOrder,
                request.StartDate,
                request.EndDate,
                request.IsActive,
                UserId = userId
            });
        }
    }

    public async Task<bool> UpdateImageAsync(int promotionId, string imageFileName)
    {
        var sql = @"
UPDATE MPromotion
SET ImageFileName = @ImageFileName,
    UpdatedAt = GETDATE()
WHERE PromotionId = @PromotionId AND IsDeleted = 0";

        var rows = await _connection.ExecuteAsync(sql, new { PromotionId = promotionId, ImageFileName = imageFileName });
        return rows > 0;
    }

    public async Task<bool> UpdateDisplayOrderAsync(int promotionId, int displayOrder)
    {
        var sql = @"
UPDATE MPromotion
SET DisplayOrder = @DisplayOrder,
    UpdatedAt = GETDATE()
WHERE PromotionId = @PromotionId AND IsDeleted = 0";

        var rows = await _connection.ExecuteAsync(sql, new { PromotionId = promotionId, DisplayOrder = displayOrder });
        return rows > 0;
    }

    public async Task<bool> ToggleActiveAsync(int promotionId, bool isActive)
    {
        var sql = @"
UPDATE MPromotion
SET IsActive = @IsActive,
    UpdatedAt = GETDATE()
WHERE PromotionId = @PromotionId AND IsDeleted = 0";

        var rows = await _connection.ExecuteAsync(sql, new { PromotionId = promotionId, IsActive = isActive });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int promotionId)
    {
        var sql = @"
UPDATE MPromotion
SET IsDeleted = 1,
    UpdatedAt = GETDATE()
WHERE PromotionId = @PromotionId";

        var rows = await _connection.ExecuteAsync(sql, new { PromotionId = promotionId });
        return rows > 0;
    }
}
