using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/promotions")]
[ApiVersion("1.0")]
[Authorize]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly ILogger<PromotionsController> _logger;

    private const long MaxImageUploadBytes = 5 * 1024 * 1024; // 5MB

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    public class PromotionFormRequest : PromotionUpsertRequest
    {
        public IFormFile? ImageFile { get; set; }
    }

    public PromotionsController(
        IPromotionRepository promotionRepository,
        ILogger<PromotionsController> logger)
    {
        _promotionRepository = promotionRepository;
        _logger = logger;
    }

    #region URL Builder

    private string? BuildImageUrl(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        if (Uri.TryCreate(fileName, UriKind.Absolute, out _)) return fileName;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/images/promotions/{fileName.TrimStart('/')}";
    }

    #endregion

    #region CRM Endpoints

    /// <summary>
    /// Get all promotions (for CRM)
    /// </summary>
    [HttpGet]
    [Permission("Promotions.Read")]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive = null)
    {
        var promotions = await _promotionRepository.GetAllAsync(isActive);
        
        foreach (var promo in promotions)
        {
            promo.ImageUrl = BuildImageUrl(promo.ImageFileName);
        }
        
        return Ok(promotions);
    }

    /// <summary>
    /// Get promotion by ID
    /// </summary>
    [HttpGet("{promotionId:int}")]
    [Permission("Promotions.Read")]
    public async Task<IActionResult> GetById([FromRoute] int promotionId)
    {
        var promotion = await _promotionRepository.GetByIdAsync(promotionId);
        if (promotion == null)
            return NotFound(new { message = $"Promotion with ID {promotionId} not found" });

        promotion.ImageUrl = BuildImageUrl(promotion.ImageFileName);
        return Ok(promotion);
    }

    /// <summary>
    /// Create new promotion
    /// </summary>
    [HttpPost]
    [Permission("Promotions.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    public async Task<IActionResult> Create([FromForm] PromotionFormRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required" });

        // Get current user ID
        var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
        int? userId = int.TryParse(userIdClaim, out var uid) ? uid : null;

        // Create promotion
        var promotionId = await _promotionRepository.UpsertAsync(request, userId);

        // Handle image upload
        if (request.ImageFile != null && request.ImageFile.Length > 0)
        {
            var imageResult = await SaveImageAsync(promotionId, request.ImageFile);
            if (!imageResult.Success)
                return BadRequest(new { message = imageResult.Error });
        }

        var created = await _promotionRepository.GetByIdAsync(promotionId);
        if (created != null)
        {
            created.ImageUrl = BuildImageUrl(created.ImageFileName);
        }

        return Ok(new { promotionId, message = "Promotion created successfully", promotion = created });
    }

    /// <summary>
    /// Update promotion
    /// </summary>
    [HttpPut("{promotionId:int}")]
    [Permission("Promotions.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    public async Task<IActionResult> Update([FromRoute] int promotionId, [FromForm] PromotionFormRequest request)
    {
        var existing = await _promotionRepository.GetByIdAsync(promotionId);
        if (existing == null)
            return NotFound(new { message = $"Promotion with ID {promotionId} not found" });

        request.PromotionId = promotionId;

        // Get current user ID
        var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
        int? userId = int.TryParse(userIdClaim, out var uid) ? uid : null;

        await _promotionRepository.UpsertAsync(request, userId);

        // Handle image upload if provided
        if (request.ImageFile != null && request.ImageFile.Length > 0)
        {
            var imageResult = await SaveImageAsync(promotionId, request.ImageFile);
            if (!imageResult.Success)
                return BadRequest(new { message = imageResult.Error });
        }

        var updated = await _promotionRepository.GetByIdAsync(promotionId);
        if (updated != null)
        {
            updated.ImageUrl = BuildImageUrl(updated.ImageFileName);
        }

        return Ok(new { promotionId, message = "Promotion updated successfully", promotion = updated });
    }

    /// <summary>
    /// Upload/Update promotion image
    /// </summary>
    [HttpPost("{promotionId:int}/image")]
    [Permission("Promotions.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    public async Task<IActionResult> UploadImage([FromRoute] int promotionId, IFormFile file)
    {
        var existing = await _promotionRepository.GetByIdAsync(promotionId);
        if (existing == null)
            return NotFound(new { message = $"Promotion with ID {promotionId} not found" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Image file is required" });

        var result = await SaveImageAsync(promotionId, file);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new 
        { 
            promotionId, 
            imageFileName = result.FileName,
            imageUrl = BuildImageUrl(result.FileName),
            message = "Image uploaded successfully" 
        });
    }

    /// <summary>
    /// Toggle promotion active status
    /// </summary>
    [HttpPatch("{promotionId:int}/toggle-active")]
    [Permission("Promotions.Manage")]
    public async Task<IActionResult> ToggleActive([FromRoute] int promotionId, [FromQuery] bool isActive)
    {
        var existing = await _promotionRepository.GetByIdAsync(promotionId);
        if (existing == null)
            return NotFound(new { message = $"Promotion with ID {promotionId} not found" });

        await _promotionRepository.ToggleActiveAsync(promotionId, isActive);

        return Ok(new { promotionId, isActive, message = $"Promotion {(isActive ? "activated" : "deactivated")} successfully" });
    }

    /// <summary>
    /// Update display order
    /// </summary>
    [HttpPatch("{promotionId:int}/order")]
    [Permission("Promotions.Manage")]
    public async Task<IActionResult> UpdateOrder([FromRoute] int promotionId, [FromQuery] int displayOrder)
    {
        var existing = await _promotionRepository.GetByIdAsync(promotionId);
        if (existing == null)
            return NotFound(new { message = $"Promotion with ID {promotionId} not found" });

        await _promotionRepository.UpdateDisplayOrderAsync(promotionId, displayOrder);

        return Ok(new { promotionId, displayOrder, message = "Display order updated successfully" });
    }

    /// <summary>
    /// Delete promotion
    /// </summary>
    [HttpDelete("{promotionId:int}")]
    [Permission("Promotions.Manage")]
    public async Task<IActionResult> Delete([FromRoute] int promotionId)
    {
        var existing = await _promotionRepository.GetByIdAsync(promotionId);
        if (existing == null)
            return NotFound(new { message = $"Promotion with ID {promotionId} not found" });

        await _promotionRepository.DeleteAsync(promotionId);

        return Ok(new { promotionId, message = "Promotion deleted successfully" });
    }

    #endregion

    #region Mobile App Endpoints

    /// <summary>
    /// Get active promotions for mobile slider (public endpoint)
    /// </summary>
    [HttpGet("slider")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSlider()
    {
        var promotions = await _promotionRepository.GetActiveForMobileAsync();
        
        foreach (var promo in promotions)
        {
            promo.ImageUrl = BuildImageUrl(promo.ImageUrl);
        }
        
        return Ok(promotions);
    }

    #endregion

    #region Helper Methods

    private async Task<(bool Success, string? FileName, string? Error)> SaveImageAsync(int promotionId, IFormFile file)
    {
        try
        {
            // Validate file size
            if (file.Length > MaxImageUploadBytes)
                return (false, null, $"Image file size exceeds maximum allowed size of {MaxImageUploadBytes / (1024 * 1024)}MB");

            // Validate extension
            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImageExtensions.Contains(extension))
                return (false, null, $"Invalid file extension. Allowed: {string.Join(", ", AllowedImageExtensions)}");

            // Generate unique filename
            var fileName = $"promo_{promotionId}_{Guid.NewGuid():N}{extension}";
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "promotions");

            // Ensure directory exists
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update database
            await _promotionRepository.UpdateImageAsync(promotionId, fileName);

            _logger.LogInformation("Saved promotion image: {FileName} for PromotionId: {PromotionId}", fileName, promotionId);

            return (true, fileName, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving promotion image for PromotionId: {PromotionId}", promotionId);
            return (false, null, "Error saving image file");
        }
    }

    #endregion
}
