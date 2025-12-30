using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/facility-services")]
[ApiVersion("1.0")]
[Authorize]
public class FacilityServicesController : ControllerBase
{
    private readonly IFacilityServiceRepository _facilityServiceRepository;
    private readonly ILogger<FacilityServicesController> _logger;

    private const long MaxUploadBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public class FacilityServiceFormRequest : FacilityServiceUpsertRequest
    {
        public IFormFile? DisplayImageFile { get; set; }
        public IFormFile? IconImageFile { get; set; }
    }

    public FacilityServicesController(IFacilityServiceRepository facilityServiceRepository, ILogger<FacilityServicesController> logger)
    {
        _facilityServiceRepository = facilityServiceRepository;
        _logger = logger;
    }

    private string? BuildServiceDisplayImageUrl(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        if (Uri.TryCreate(fileName, UriKind.Absolute, out _))
            return fileName;

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/images/services/{fileName.TrimStart('/')}";
    }

    private string? BuildServiceIconUrl(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        if (Uri.TryCreate(fileName, UriKind.Absolute, out _))
            return fileName;

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var iconPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "service-icons", fileName);
        if (System.IO.File.Exists(iconPath))
            return $"{baseUrl}/images/service-icons/{fileName.TrimStart('/')}";

        return $"{baseUrl}/images/services/{fileName.TrimStart('/')}";
    }

    [HttpGet]
    [Permission("FacilityServices.Read")]
    public async Task<IActionResult> GetAll([FromQuery] int? facilityId = null, [FromQuery] bool includeInactive = false)
    {
        var rows = await _facilityServiceRepository.GetAllAsync(facilityId, includeInactive);
        foreach (var r in rows)
        {
            r.DisplayImageName = BuildServiceDisplayImageUrl(r.DisplayImageName);
            r.IconImageName = BuildServiceIconUrl(r.IconImageName);
        }

        return Ok(rows);
    }

    [HttpGet("{serviceId:int}")]
    [Permission("FacilityServices.Read")]
    public async Task<IActionResult> GetById([FromRoute] int serviceId)
    {
        var row = await _facilityServiceRepository.GetByIdAsync(serviceId);
        if (row == null)
            return NotFound(new { message = $"Service with ID {serviceId} not found" });

        row.DisplayImageName = BuildServiceDisplayImageUrl(row.DisplayImageName);
        row.IconImageName = BuildServiceIconUrl(row.IconImageName);

        return Ok(row);
    }

    [HttpPost]
    [Permission("FacilityServices.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Create([FromForm] FacilityServiceFormRequest request)
    {
        request.SId = null;
        var id = await _facilityServiceRepository.UpsertAsync(request);

        if (request.DisplayImageFile != null)
        {
            var result = await SaveServiceImageAsync(id, request.DisplayImageFile, isIcon: false);
            if (result.Result != null)
                return result.Result;
        }

        if (request.IconImageFile != null)
        {
            var result = await SaveServiceImageAsync(id, request.IconImageFile, isIcon: true);
            if (result.Result != null)
                return result.Result;
        }

        var updated = await _facilityServiceRepository.GetByIdAsync(id);
        if (updated != null)
        {
            updated.DisplayImageName = BuildServiceDisplayImageUrl(updated.DisplayImageName);
            updated.IconImageName = BuildServiceIconUrl(updated.IconImageName);
        }
        return Ok(new { serviceId = id, row = updated });
    }

    [HttpDelete("{serviceId:int}")]
    [Permission("FacilityServices.Manage")]
    public async Task<IActionResult> Delete([FromRoute] int serviceId)
    {
        var existing = await _facilityServiceRepository.GetByIdAsync(serviceId);
        if (existing == null)
            return NotFound(new { message = $"Service with ID {serviceId} not found" });

        var deleted = await _facilityServiceRepository.DeleteAsync(serviceId);
        if (!deleted)
            return StatusCode(500, new { message = "Failed to delete service" });

        return Ok(new { serviceId });
    }

    [HttpPut("{serviceId:int}")]
    [Permission("FacilityServices.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Update([FromRoute] int serviceId, [FromForm] FacilityServiceFormRequest request)
    {
        request.SId = serviceId;

        var existing = await _facilityServiceRepository.GetByIdAsync(serviceId);
        if (existing == null)
            return NotFound(new { message = $"Service with ID {serviceId} not found" });

        request.DisplayImageName ??= existing.DisplayImageName;
        request.IconImageName ??= existing.IconImageName;

        var id = await _facilityServiceRepository.UpsertAsync(request);

        if (request.DisplayImageFile != null)
        {
            var result = await SaveServiceImageAsync(id, request.DisplayImageFile, isIcon: false);
            if (result.Result != null)
                return result.Result;
        }

        if (request.IconImageFile != null)
        {
            var result = await SaveServiceImageAsync(id, request.IconImageFile, isIcon: true);
            if (result.Result != null)
                return result.Result;
        }

        var updated = await _facilityServiceRepository.GetByIdAsync(id);
        if (updated != null)
        {
            updated.DisplayImageName = BuildServiceDisplayImageUrl(updated.DisplayImageName);
            updated.IconImageName = BuildServiceIconUrl(updated.IconImageName);
        }
        return Ok(new { serviceId = id, row = updated });
    }

    private async Task<(IActionResult? Result, string? FileName, string? VirtualPath)> SaveServiceImageAsync(int serviceId, IFormFile file, bool isIcon)
    {
        if (file.Length <= 0)
            return (BadRequest(new { message = "File is empty" }), null, null);

        if (file.Length > MaxUploadBytes)
            return (BadRequest(new { message = "File is too large. Max allowed is 5MB" }), null, null);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            return (BadRequest(new { message = "Invalid file type. Allowed: jpg, jpeg, png, webp" }), null, null);

        var exists = await _facilityServiceRepository.GetByIdAsync(serviceId);
        if (exists == null)
            return (NotFound(new { message = $"Service with ID {serviceId} not found" }), null, null);

        var root = isIcon
            ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "service-icons")
            : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "services");
        Directory.CreateDirectory(root);

        string fileName;
        await using (var stream = file.OpenReadStream())
        {
            var hash = await SHA256.HashDataAsync(stream);
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();
            var prefix = isIcon ? "service_icon" : "service_display";
            fileName = $"{prefix}_{serviceId}_{hashString}{extension.ToLowerInvariant()}";
        }

        var fullPath = Path.Combine(root, fileName);

        try
        {
            await using var fs = System.IO.File.Create(fullPath);
            await file.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save service image. ServiceId: {ServiceId}", serviceId);
            return (StatusCode(500, new { message = "Failed to save file" }), null, null);
        }

        var updated = isIcon
            ? await _facilityServiceRepository.UpdateIconImageAsync(serviceId, fileName)
            : await _facilityServiceRepository.UpdateDisplayImageAsync(serviceId, fileName);

        if (!updated)
            return (StatusCode(500, new { message = "Failed to update service image name" }), null, null);

        var virtualPath = isIcon ? $"/images/icons/{fileName}" : $"/images/services/{fileName}";
        return (null, fileName, virtualPath);
    }
}
