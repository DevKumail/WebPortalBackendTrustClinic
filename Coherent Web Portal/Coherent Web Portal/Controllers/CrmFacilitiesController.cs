using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Cryptography;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/facilities")]
[ApiVersion("1.0")]
[Authorize]
public class CrmFacilitiesController : ControllerBase
{
    private readonly ICrmFacilityRepository _crmFacilityRepository;

    private readonly ILogger<CrmFacilitiesController> _logger;

    private const long MaxUploadBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public class FacilityImagesUploadRequest
    {
        public List<IFormFile>? Files { get; set; }
    }

    public class FacilityUpsertFormRequest : CrmFacilityUpsertRequest
    {
        public List<IFormFile>? Files { get; set; }
    }

    public CrmFacilitiesController(ICrmFacilityRepository crmFacilityRepository, ILogger<CrmFacilitiesController> logger)
    {
        _crmFacilityRepository = crmFacilityRepository;
        _logger = logger;
    }

    [HttpGet]
    [Permission("Facilities.Read")]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var rows = await _crmFacilityRepository.GetAllAsync(includeInactive);
        return Ok(rows);
    }

    [HttpGet("dropdown")]
    [Permission("Facilities.Read")]
    public async Task<IActionResult> GetDropdown()
    {
        var rows = await _crmFacilityRepository.GetDropdownAsync();
        return Ok(rows);
    }

    [HttpGet("{facilityId:int}")]
    [Permission("Facilities.Read")]
    public async Task<IActionResult> GetById([FromRoute] int facilityId)
    {
        var row = await _crmFacilityRepository.GetByIdAsync(facilityId);
        if (row == null)
            return NotFound(new { message = $"Facility with ID {facilityId} not found" });

        return Ok(row);
    }

    [HttpPost]
    [Permission("Facilities.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Create([FromForm] FacilityUpsertFormRequest request)
    {
        request.FId = null;
        var id = await _crmFacilityRepository.UpsertAsync(request);

        if (request.Files != null && request.Files.Count > 0)
        {
            var result = await SaveFacilityImagesAsync(id, request.Files);
            if (result.Result != null)
                return result.Result;
        }

        var updated = await _crmFacilityRepository.GetByIdAsync(id);
        return Ok(new { facilityId = id, row = updated });
    }

    [HttpDelete("{facilityId:int}")]
    [Permission("Facilities.Manage")]
    public async Task<IActionResult> Delete([FromRoute] int facilityId)
    {
        var existing = await _crmFacilityRepository.GetByIdAsync(facilityId);
        if (existing == null)
            return NotFound(new { message = $"Facility with ID {facilityId} not found" });

        try
        {
            var deleted = await _crmFacilityRepository.DeleteAsync(facilityId);
            if (!deleted)
                return StatusCode(500, new { message = "Failed to delete facility" });

            return Ok(new { facilityId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete facility. FacilityId: {FacilityId}", facilityId);
            return StatusCode(500, new { message = "Failed to delete facility" });
        }
    }

    [HttpPut("{facilityId:int}")]
    [Permission("Facilities.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Update([FromRoute] int facilityId, [FromForm] FacilityUpsertFormRequest request)
    {
        request.FId = facilityId;

        var existing = await _crmFacilityRepository.GetByIdAsync(facilityId);
        if (existing == null)
            return NotFound(new { message = $"Facility with ID {facilityId} not found" });

        request.FacilityImages ??= existing.FacilityImages;

        var id = await _crmFacilityRepository.UpsertAsync(request);
        if (request.Files != null && request.Files.Count > 0)
        {
            var result = await SaveFacilityImagesAsync(id, request.Files);
            if (result.Result != null)
                return result.Result;
        }

        var updated = await _crmFacilityRepository.GetByIdAsync(id);
        return Ok(new { facilityId = id, row = updated });
    }

    private async Task<(IActionResult? Result, string? FacilityImages)> SaveFacilityImagesAsync(int facilityId, List<IFormFile> files)
    {
        if (files.Count == 0)
            return (BadRequest(new { message = "Files are required" }), null);

        var facility = await _crmFacilityRepository.GetByIdAsync(facilityId);
        if (facility == null)
            return (NotFound(new { message = $"Facility with ID {facilityId} not found" }), null);

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "facilities");
        Directory.CreateDirectory(root);

        var newFileNames = new List<string>();

        foreach (var file in files)
        {
            if (file == null)
                continue;

            if (file.Length <= 0)
                return (BadRequest(new { message = "One of the files is empty" }), null);

            if (file.Length > MaxUploadBytes)
                return (BadRequest(new { message = "One of the files is too large. Max allowed is 5MB" }), null);

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                return (BadRequest(new { message = "Invalid file type. Allowed: jpg, jpeg, png, webp" }), null);

            string fileName;
            await using (var stream = file.OpenReadStream())
            {
                var hash = await SHA256.HashDataAsync(stream);
                var hashString = Convert.ToHexString(hash).ToLowerInvariant();
                fileName = $"facility_{facilityId}_{hashString}{extension.ToLowerInvariant()}";
            }

            var fullPath = Path.Combine(root, fileName);

            try
            {
                await using var fs = System.IO.File.Create(fullPath);
                await file.CopyToAsync(fs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save facility image. FacilityId: {FacilityId}", facilityId);
                return (StatusCode(500, new { message = "Failed to save file" }), null);
            }

            newFileNames.Add(fileName);
        }

        var existing = (facility.FacilityImages ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        existing.AddRange(newFileNames);

        var updatedImages = string.Join(',', existing.Distinct(StringComparer.OrdinalIgnoreCase));
        var updated = await _crmFacilityRepository.UpdateFacilityImagesAsync(facilityId, updatedImages);
        if (!updated)
            return (StatusCode(500, new { message = "Failed to update facility images" }), null);

        return (null, updatedImages);
    }
}
