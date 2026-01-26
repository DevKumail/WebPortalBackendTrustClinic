using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

public interface IPromotionRepository
{
    Task<List<PromotionListDto>> GetAllAsync(bool? isActive = null);
    Task<List<PromotionSliderDto>> GetActiveForMobileAsync();
    Task<PromotionDetailDto?> GetByIdAsync(int promotionId);
    Task<int> UpsertAsync(PromotionUpsertRequest request, int? userId);
    Task<bool> UpdateImageAsync(int promotionId, string imageFileName);
    Task<bool> UpdateDisplayOrderAsync(int promotionId, int displayOrder);
    Task<bool> ToggleActiveAsync(int promotionId, bool isActive);
    Task<bool> DeleteAsync(int promotionId);
}
