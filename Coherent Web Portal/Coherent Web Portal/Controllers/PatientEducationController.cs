using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/patient-education")]
[ApiVersion("1.0")]
[Authorize]
public class PatientEducationController : ControllerBase
{
    private readonly IPatientEducationRepository _educationRepository;
    private readonly IPatientEducationCategoryRepository _categoryRepository;
    private readonly ILogger<PatientEducationController> _logger;

    private const long MaxImageUploadBytes = 5 * 1024 * 1024; // 5MB
    private const long MaxPdfUploadBytes = 20 * 1024 * 1024; // 20MB

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private static readonly HashSet<string> AllowedPdfExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf"
    };

    public class EducationFormRequest : PatientEducationUpsertRequest
    {
        public IFormFile? ThumbnailFile { get; set; }
        public IFormFile? PdfFile { get; set; }
        public bool? RemovePdf { get; set; }
    }

    public class CategoryFormRequest : PatientEducationCategoryUpsertRequest
    {
        public IFormFile? IconImageFile { get; set; }
    }

    public PatientEducationController(
        IPatientEducationRepository educationRepository,
        IPatientEducationCategoryRepository categoryRepository,
        ILogger<PatientEducationController> logger)
    {
        _educationRepository = educationRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    #region URL Builders

    private string? BuildThumbnailUrl(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        if (Uri.TryCreate(fileName, UriKind.Absolute, out _)) return fileName;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/images/education/thumbnails/{fileName.TrimStart('/')}";
    }

    private string? BuildPdfUrl(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return null;
        if (Uri.TryCreate(filePath, UriKind.Absolute, out _)) return filePath;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/files/education/pdfs/{filePath.TrimStart('/')}";
    }

    private string? BuildCategoryIconUrl(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        if (Uri.TryCreate(fileName, UriKind.Absolute, out _)) return fileName;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/images/education/icons/{fileName.TrimStart('/')}";
    }

    private string? BuildContentImageUrl(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        if (Uri.TryCreate(fileName, UriKind.Absolute, out _)) return fileName;
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/images/education/content/{fileName.TrimStart('/')}";
    }

    #endregion

    #region Category Endpoints

    /// <summary>
    /// Get all education categories
    /// </summary>
    [HttpGet("categories")]
    [Permission("PatientEducation.Read")]
    public async Task<IActionResult> GetAllCategories([FromQuery] bool includeInactive = false)
    {
        var rows = await _categoryRepository.GetAllAsync(includeInactive);
        foreach (var r in rows)
        {
            r.IconImageName = BuildCategoryIconUrl(r.IconImageName);
        }
        return Ok(rows);
    }

    /// <summary>
    /// Get category dropdown list
    /// </summary>
    [HttpGet("categories/dropdown")]
    [Permission("PatientEducation.Read")]
    public async Task<IActionResult> GetCategoryDropdown()
    {
        var rows = await _categoryRepository.GetDropdownListAsync();
        return Ok(rows);
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    [HttpGet("categories/{categoryId:int}")]
    [Permission("PatientEducation.Read")]
    public async Task<IActionResult> GetCategoryById([FromRoute] int categoryId)
    {
        var row = await _categoryRepository.GetByIdAsync(categoryId);
        if (row == null)
            return NotFound(new { message = $"Category with ID {categoryId} not found" });

        return Ok(new
        {
            categoryId = row.CategoryId,
            categoryName = row.CategoryName,
            arCategoryName = row.ArCategoryName,
            categoryDescription = row.CategoryDescription,
            arCategoryDescription = row.ArCategoryDescription,
            iconImageName = BuildCategoryIconUrl(row.IconImageName),
            displayOrder = row.DisplayOrder,
            isGeneral = row.IsGeneral,
            active = row.Active
        });
    }

    /// <summary>
    /// Create a new education category
    /// </summary>
    [HttpPost("categories")]
    [Permission("PatientEducation.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    public async Task<IActionResult> CreateCategory([FromForm] CategoryFormRequest request)
    {
        request.CategoryId = null;
        var id = await _categoryRepository.UpsertAsync(request);

        if (request.IconImageFile != null)
        {
            var result = await SaveCategoryIconAsync(id, request.IconImageFile);
            if (result.Result != null)
                return result.Result;
        }

        var updated = await _categoryRepository.GetByIdAsync(id);
        return Ok(new
        {
            categoryId = id,
            row = updated != null ? new
            {
                categoryId = updated.CategoryId,
                categoryName = updated.CategoryName,
                arCategoryName = updated.ArCategoryName,
                categoryDescription = updated.CategoryDescription,
                arCategoryDescription = updated.ArCategoryDescription,
                iconImageName = BuildCategoryIconUrl(updated.IconImageName),
                displayOrder = updated.DisplayOrder,
                isGeneral = updated.IsGeneral,
                active = updated.Active
            } : null
        });
    }

    /// <summary>
    /// Update an education category
    /// </summary>
    [HttpPut("categories/{categoryId:int}")]
    [Permission("PatientEducation.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    public async Task<IActionResult> UpdateCategory([FromRoute] int categoryId, [FromForm] CategoryFormRequest request)
    {
        request.CategoryId = categoryId;

        var existing = await _categoryRepository.GetByIdAsync(categoryId);
        if (existing == null)
            return NotFound(new { message = $"Category with ID {categoryId} not found" });

        var id = await _categoryRepository.UpsertAsync(request);

        if (request.IconImageFile != null)
        {
            var result = await SaveCategoryIconAsync(id, request.IconImageFile);
            if (result.Result != null)
                return result.Result;
        }

        var updated = await _categoryRepository.GetByIdAsync(id);
        return Ok(new
        {
            categoryId = id,
            row = updated != null ? new
            {
                categoryId = updated.CategoryId,
                categoryName = updated.CategoryName,
                arCategoryName = updated.ArCategoryName,
                categoryDescription = updated.CategoryDescription,
                arCategoryDescription = updated.ArCategoryDescription,
                iconImageName = BuildCategoryIconUrl(updated.IconImageName),
                displayOrder = updated.DisplayOrder,
                isGeneral = updated.IsGeneral,
                active = updated.Active
            } : null
        });
    }

    /// <summary>
    /// Delete an education category
    /// </summary>
    [HttpDelete("categories/{categoryId:int}")]
    [Permission("PatientEducation.Manage")]
    public async Task<IActionResult> DeleteCategory([FromRoute] int categoryId)
    {
        var existing = await _categoryRepository.GetByIdAsync(categoryId);
        if (existing == null)
            return NotFound(new { message = $"Category with ID {categoryId} not found" });

        var deleted = await _categoryRepository.DeleteAsync(categoryId);
        if (!deleted)
            return StatusCode(500, new { message = "Failed to delete category" });

        return Ok(new { categoryId });
    }

    /// <summary>
    /// Upload icon image for a category
    /// </summary>
    [HttpPost("categories/{categoryId:int}/icon")]
    [Permission("PatientEducation.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    public async Task<IActionResult> UploadCategoryIcon([FromRoute] int categoryId, IFormFile file)
    {
        var result = await SaveCategoryIconAsync(categoryId, file);
        if (result.Result != null)
            return result.Result;

        return Ok(new
        {
            categoryId,
            iconImageName = result.FileName,
            iconImageUrl = BuildCategoryIconUrl(result.FileName)
        });
    }

    #endregion

    #region Education Content Endpoints

    /// <summary>
    /// Get all education content
    /// </summary>
    [HttpGet]
    [Permission("PatientEducation.Read")]
    public async Task<IActionResult> GetAllEducation(
        [FromQuery] int? categoryId = null,
        [FromQuery] bool includeInactive = false)
    {
        var rows = await _educationRepository.GetAllAsync(categoryId, includeInactive);
        foreach (var r in rows)
        {
            r.ThumbnailImageName = BuildThumbnailUrl(r.ThumbnailImageName);
        }
        return Ok(rows);
    }

    /// <summary>
    /// Get education content by ID
    /// </summary>
    [HttpGet("{educationId:int}")]
    [Permission("PatientEducation.Read")]
    public async Task<IActionResult> GetEducationById([FromRoute] int educationId)
    {
        var row = await _educationRepository.GetDetailByIdAsync(educationId);
        if (row == null)
            return NotFound(new { message = $"Education content with ID {educationId} not found" });

        row.ThumbnailImageUrl = BuildThumbnailUrl(row.ThumbnailImageName);
        row.PdfFileUrl = BuildPdfUrl(row.PdfFileUrl);

        return Ok(row);
    }

    /// <summary>
    /// Create new education content with Delta JSON (like MS Word editor with inline images)
    /// </summary>
    [HttpPost]
    [Permission("PatientEducation.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxPdfUploadBytes)]
    public async Task<IActionResult> CreateEducation([FromForm] EducationFormRequest request)
    {
        request.EducationId = null;

        // Process base64 images in Delta JSON and convert to file URLs
        request.ContentDeltaJson = await ProcessDeltaJsonBase64ImagesAsync(request.ContentDeltaJson);
        request.ArContentDeltaJson = await ProcessDeltaJsonBase64ImagesAsync(request.ArContentDeltaJson);

        var id = await _educationRepository.UpsertAsync(request);

        // Handle thumbnail upload
        if (request.ThumbnailFile != null)
        {
            var result = await SaveThumbnailAsync(id, request.ThumbnailFile);
            if (result.Result != null)
                return result.Result;
        }

        // Handle PDF upload
        if (request.PdfFile != null)
        {
            var result = await SavePdfAsync(id, request.PdfFile);
            if (result.Result != null)
                return result.Result;
        }

        var updated = await _educationRepository.GetDetailByIdAsync(id);
        if (updated != null)
        {
            updated.ThumbnailImageUrl = BuildThumbnailUrl(updated.ThumbnailImageName);
            updated.PdfFileUrl = BuildPdfUrl(updated.PdfFileUrl);
        }

        return Ok(new { educationId = id, row = updated });
    }

    /// <summary>
    /// Update education content with Delta JSON
    /// </summary>
    [HttpPut("{educationId:int}")]
    [Permission("PatientEducation.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxPdfUploadBytes)]
    public async Task<IActionResult> UpdateEducation([FromRoute] int educationId, [FromForm] EducationFormRequest request)
    {
        request.EducationId = educationId;

        var existing = await _educationRepository.GetByIdAsync(educationId);
        if (existing == null)
            return NotFound(new { message = $"Education content with ID {educationId} not found" });

        // Process base64 images in Delta JSON and convert to file URLs
        request.ContentDeltaJson = await ProcessDeltaJsonBase64ImagesAsync(request.ContentDeltaJson);
        request.ArContentDeltaJson = await ProcessDeltaJsonBase64ImagesAsync(request.ArContentDeltaJson);

        var id = await _educationRepository.UpsertAsync(request);

        // Handle file uploads
        if (request.ThumbnailFile != null)
        {
            var result = await SaveThumbnailAsync(id, request.ThumbnailFile);
            if (result.Result != null)
                return result.Result;
        }

        // Handle PDF removal
        if (request.RemovePdf == true)
        {
            await _educationRepository.RemovePdfAsync(id);
        }
        // Handle PDF upload
        else if (request.PdfFile != null)
        {
            var result = await SavePdfAsync(id, request.PdfFile);
            if (result.Result != null)
                return result.Result;
        }

        var updated = await _educationRepository.GetDetailByIdAsync(id);
        if (updated != null)
        {
            updated.ThumbnailImageUrl = BuildThumbnailUrl(updated.ThumbnailImageName);
            updated.PdfFileUrl = BuildPdfUrl(updated.PdfFileUrl);
        }

        return Ok(new { educationId = id, row = updated });
    }

    /// <summary>
    /// Delete education content
    /// </summary>
    [HttpDelete("{educationId:int}")]
    [Permission("PatientEducation.Manage")]
    public async Task<IActionResult> DeleteEducation([FromRoute] int educationId)
    {
        var existing = await _educationRepository.GetByIdAsync(educationId);
        if (existing == null)
            return NotFound(new { message = $"Education content with ID {educationId} not found" });

        var deleted = await _educationRepository.DeleteAsync(educationId);
        if (!deleted)
            return StatusCode(500, new { message = "Failed to delete education content" });

        return Ok(new { educationId });
    }

    /// <summary>
    /// Upload thumbnail image for education content
    /// </summary>
    [HttpPost("{educationId:int}/thumbnail")]
    [Permission("PatientEducation.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    public async Task<IActionResult> UploadThumbnail([FromRoute] int educationId, IFormFile file)
    {
        var result = await SaveThumbnailAsync(educationId, file);
        if (result.Result != null)
            return result.Result;

        return Ok(new
        {
            educationId,
            thumbnailImageName = result.FileName,
            thumbnailImageUrl = BuildThumbnailUrl(result.FileName)
        });
    }

    /// <summary>
    /// Upload PDF document for education content (separate, downloadable)
    /// </summary>
    [HttpPost("{educationId:int}/pdf")]
    [Permission("PatientEducation.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxPdfUploadBytes)]
    public async Task<IActionResult> UploadPdf([FromRoute] int educationId, IFormFile file)
    {
        var result = await SavePdfAsync(educationId, file);
        if (result.Result != null)
            return result.Result;

        return Ok(new
        {
            educationId,
            pdfFileName = result.FileName,
            pdfFileUrl = BuildPdfUrl(result.FileName)
        });
    }

    /// <summary>
    /// Remove PDF from education content
    /// </summary>
    [HttpDelete("{educationId:int}/pdf")]
    [Permission("PatientEducation.Manage")]
    public async Task<IActionResult> RemovePdf([FromRoute] int educationId)
    {
        var existing = await _educationRepository.GetByIdAsync(educationId);
        if (existing == null)
            return NotFound(new { message = $"Education content with ID {educationId} not found" });

        var removed = await _educationRepository.RemovePdfAsync(educationId);
        if (!removed)
            return StatusCode(500, new { message = "Failed to remove PDF" });

        return Ok(new { educationId, message = "PDF removed successfully" });
    }

    /// <summary>
    /// Upload inline image for Delta JSON editor content
    /// Returns URL to embed in Delta JSON
    /// </summary>
    [HttpPost("content-image")]
    [Permission("PatientEducation.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    public async Task<IActionResult> UploadContentImage(IFormFile file)
    {
        if (file.Length <= 0)
            return BadRequest(new { message = "File is empty" });

        if (file.Length > MaxImageUploadBytes)
            return BadRequest(new { message = "File is too large. Max allowed is 5MB" });

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            return BadRequest(new { message = "Invalid file type. Allowed: jpg, jpeg, png, webp, gif" });

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "education", "content");
        Directory.CreateDirectory(root);

        string fileName;
        await using (var stream = file.OpenReadStream())
        {
            var hash = await SHA256.HashDataAsync(stream);
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();
            fileName = $"content_{hashString}{extension.ToLowerInvariant()}";
        }

        var fullPath = Path.Combine(root, fileName);

        try
        {
            await using var fs = System.IO.File.Create(fullPath);
            await file.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save content image");
            return StatusCode(500, new { message = "Failed to save file" });
        }

        return Ok(new
        {
            fileName,
            imageUrl = BuildContentImageUrl(fileName)
        });
    }

    #endregion

    #region Private Helper Methods

    private async Task<(IActionResult? Result, string? FileName)> SaveCategoryIconAsync(int categoryId, IFormFile file)
    {
        if (file.Length <= 0)
            return (BadRequest(new { message = "File is empty" }), null);

        if (file.Length > MaxImageUploadBytes)
            return (BadRequest(new { message = "File is too large. Max allowed is 5MB" }), null);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            return (BadRequest(new { message = "Invalid file type. Allowed: jpg, jpeg, png, webp, gif" }), null);

        var exists = await _categoryRepository.GetByIdAsync(categoryId);
        if (exists == null)
            return (NotFound(new { message = $"Category with ID {categoryId} not found" }), null);

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "education", "icons");
        Directory.CreateDirectory(root);

        string fileName;
        await using (var stream = file.OpenReadStream())
        {
            var hash = await SHA256.HashDataAsync(stream);
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();
            fileName = $"category_icon_{categoryId}_{hashString}{extension.ToLowerInvariant()}";
        }

        var fullPath = Path.Combine(root, fileName);

        try
        {
            await using var fs = System.IO.File.Create(fullPath);
            await file.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save category icon. CategoryId: {CategoryId}", categoryId);
            return (StatusCode(500, new { message = "Failed to save file" }), null);
        }

        var updated = await _categoryRepository.UpdateIconImageAsync(categoryId, fileName);
        if (!updated)
            return (StatusCode(500, new { message = "Failed to update category icon" }), null);

        return (null, fileName);
    }

    private async Task<(IActionResult? Result, string? FileName)> SaveThumbnailAsync(int educationId, IFormFile file)
    {
        if (file.Length <= 0)
            return (BadRequest(new { message = "File is empty" }), null);

        if (file.Length > MaxImageUploadBytes)
            return (BadRequest(new { message = "File is too large. Max allowed is 5MB" }), null);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            return (BadRequest(new { message = "Invalid file type. Allowed: jpg, jpeg, png, webp, gif" }), null);

        var exists = await _educationRepository.GetByIdAsync(educationId);
        if (exists == null)
            return (NotFound(new { message = $"Education content with ID {educationId} not found" }), null);

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "education", "thumbnails");
        Directory.CreateDirectory(root);

        string fileName;
        await using (var stream = file.OpenReadStream())
        {
            var hash = await SHA256.HashDataAsync(stream);
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();
            fileName = $"edu_thumb_{educationId}_{hashString}{extension.ToLowerInvariant()}";
        }

        var fullPath = Path.Combine(root, fileName);

        try
        {
            await using var fs = System.IO.File.Create(fullPath);
            await file.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save thumbnail. EducationId: {EducationId}", educationId);
            return (StatusCode(500, new { message = "Failed to save file" }), null);
        }

        var updated = await _educationRepository.UpdateThumbnailAsync(educationId, fileName);
        if (!updated)
            return (StatusCode(500, new { message = "Failed to update thumbnail" }), null);

        return (null, fileName);
    }

    private async Task<(IActionResult? Result, string? FileName)> SavePdfAsync(int educationId, IFormFile file)
    {
        if (file.Length <= 0)
            return (BadRequest(new { message = "File is empty" }), null);

        if (file.Length > MaxPdfUploadBytes)
            return (BadRequest(new { message = "File is too large. Max allowed is 20MB" }), null);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedPdfExtensions.Contains(extension))
            return (BadRequest(new { message = "Invalid file type. Only PDF is allowed" }), null);

        var exists = await _educationRepository.GetByIdAsync(educationId);
        if (exists == null)
            return (NotFound(new { message = $"Education content with ID {educationId} not found" }), null);

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files", "education", "pdfs");
        Directory.CreateDirectory(root);

        string fileName;
        await using (var stream = file.OpenReadStream())
        {
            var hash = await SHA256.HashDataAsync(stream);
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();
            fileName = $"edu_pdf_{educationId}_{hashString}{extension.ToLowerInvariant()}";
        }

        var fullPath = Path.Combine(root, fileName);

        try
        {
            await using var fs = System.IO.File.Create(fullPath);
            await file.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save PDF. EducationId: {EducationId}", educationId);
            return (StatusCode(500, new { message = "Failed to save file" }), null);
        }

        var updated = await _educationRepository.UpdatePdfAsync(educationId, fileName, fileName);
        if (!updated)
            return (StatusCode(500, new { message = "Failed to update PDF" }), null);

        return (null, fileName);
    }

    /// <summary>
    /// Process Delta JSON content and extract base64 images, save them as files, and replace with URLs
    /// </summary>
    private async Task<string?> ProcessDeltaJsonBase64ImagesAsync(string? deltaJson)
    {
        if (string.IsNullOrWhiteSpace(deltaJson))
            return deltaJson;

        try
        {
            using var doc = JsonDocument.Parse(deltaJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("ops", out var ops) || ops.ValueKind != JsonValueKind.Array)
                return deltaJson;

            var modified = false;
            var newOps = new List<object>();

            foreach (var op in ops.EnumerateArray())
            {
                if (op.TryGetProperty("insert", out var insert) && insert.ValueKind == JsonValueKind.Object)
                {
                    if (insert.TryGetProperty("image", out var image) && image.ValueKind == JsonValueKind.String)
                    {
                        var imageValue = image.GetString();
                        if (!string.IsNullOrEmpty(imageValue) && imageValue.StartsWith("data:image/"))
                        {
                            // Extract and save base64 image
                            var url = await SaveBase64ImageAsync(imageValue);
                            if (!string.IsNullOrEmpty(url))
                            {
                                modified = true;
                                // Create new op with URL instead of base64
                                var newOp = new Dictionary<string, object>
                                {
                                    ["insert"] = new Dictionary<string, string> { ["image"] = url }
                                };
                                
                                // Copy attributes if present
                                if (op.TryGetProperty("attributes", out var attrs))
                                {
                                    newOp["attributes"] = JsonSerializer.Deserialize<Dictionary<string, object>>(attrs.GetRawText())!;
                                }
                                
                                newOps.Add(newOp);
                                continue;
                            }
                        }
                    }
                }
                
                // Keep original op
                newOps.Add(JsonSerializer.Deserialize<object>(op.GetRawText())!);
            }

            if (modified)
            {
                var result = new Dictionary<string, object> { ["ops"] = newOps };
                return JsonSerializer.Serialize(result);
            }

            return deltaJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Delta JSON for base64 images");
            return deltaJson;
        }
    }

    /// <summary>
    /// Save base64 image data to file and return URL
    /// </summary>
    private async Task<string?> SaveBase64ImageAsync(string base64Data)
    {
        try
        {
            // Parse data URI: data:image/png;base64,iVBORw0...
            var match = Regex.Match(base64Data, @"^data:image/(?<type>\w+);base64,(?<data>.+)$");
            if (!match.Success)
                return null;

            var imageType = match.Groups["type"].Value.ToLowerInvariant();
            var base64String = match.Groups["data"].Value;

            // Map image type to extension
            var extension = imageType switch
            {
                "jpeg" => ".jpg",
                "jpg" => ".jpg",
                "png" => ".png",
                "gif" => ".gif",
                "webp" => ".webp",
                _ => null
            };

            if (extension == null)
                return null;

            var imageBytes = Convert.FromBase64String(base64String);

            // Check size limit (5MB)
            if (imageBytes.Length > MaxImageUploadBytes)
            {
                _logger.LogWarning("Base64 image exceeds size limit: {Size} bytes", imageBytes.Length);
                return null;
            }

            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "education", "content");
            Directory.CreateDirectory(root);

            // Generate filename from hash
            var hash = SHA256.HashData(imageBytes);
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();
            var fileName = $"content_{hashString}{extension}";
            var fullPath = Path.Combine(root, fileName);

            // Only save if file doesn't exist (deduplication)
            if (!System.IO.File.Exists(fullPath))
            {
                await System.IO.File.WriteAllBytesAsync(fullPath, imageBytes);
            }

            return BuildContentImageUrl(fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save base64 image");
            return null;
        }
    }

    #endregion
}
