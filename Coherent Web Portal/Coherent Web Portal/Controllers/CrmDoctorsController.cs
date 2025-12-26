using Asp.Versioning;
using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace Coherent.Web.Portal.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/doctors")]
[ApiVersion("1.0")]
[Authorize]
public class CrmDoctorsController : ControllerBase
{
    private readonly ICrmDoctorRepository _crmDoctorRepository;
    private readonly ILogger<CrmDoctorsController> _logger;

    private const long MaxUploadBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public class DoctorPhotoUploadRequest
    {
        public IFormFile? File { get; set; }
    }

    public class DoctorCreateFormRequest : CrmDoctorUpsertRequest
    {
        public IFormFile? File { get; set; }
    }

    public CrmDoctorsController(ICrmDoctorRepository crmDoctorRepository, ILogger<CrmDoctorsController> logger)
    {
        _crmDoctorRepository = crmDoctorRepository;
        _logger = logger;
    }

    private string? BuildDoctorPhotoUrl(string? doctorPhotoName)
    {
        if (string.IsNullOrWhiteSpace(doctorPhotoName))
            return null;

        return $"{Request.Scheme}://{Request.Host}/images/doctors/{doctorPhotoName}";
    }

    [HttpGet]
    [Permission("Doctors.Read")]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var rows = await _crmDoctorRepository.GetAllAsync(includeInactive);
        return Ok(rows);
    }

    [HttpGet("{doctorId:int}")]
    [Permission("Doctors.Read")]
    public async Task<IActionResult> GetById([FromRoute] int doctorId)
    {
        var row = await _crmDoctorRepository.GetByIdAsync(doctorId);
        if (row == null)
            return NotFound(new { message = $"Doctor with ID {doctorId} not found" });

        return Ok(new
        {
            dId = row.DId,
            doctorName = row.DoctorName,
            arDoctorName = row.ArDoctorName,
            title = row.Title,
            arTitle = row.ArTitle,
            spId = row.SPId,
            yearsOfExperience = row.YearsOfExperience,
            nationality = row.Nationality,
            arNationality = row.ArNationality,
            languages = row.Languages,
            arLanguages = row.ArLanguages,
            doctorPhotoName = row.DoctorPhotoName,
            doctorPhotoUrl = BuildDoctorPhotoUrl(row.DoctorPhotoName),
            about = row.About,
            arAbout = row.ArAbout,
            education = row.Education,
            arEducation = row.ArEducation,
            experience = row.Experience,
            arExperience = row.ArExperience,
            expertise = row.Expertise,
            arExpertise = row.ArExpertise,
            licenceNo = row.LicenceNo,
            active = row.Active,
            gender = row.Gender
        });
    }

    [HttpPost]
    [Permission("Doctors.Manage")]
    [Consumes("application/json")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Create([FromBody] CrmDoctorUpsertRequest request)
    {
        request.DId = null;
        var id = await _crmDoctorRepository.UpsertAsync(request);
        return Ok(new { doctorId = id });
    }

    [HttpPost]
    [Permission("Doctors.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Create([FromForm] DoctorCreateFormRequest request)
    {
        request.DId = null;
        var id = await _crmDoctorRepository.UpsertAsync(request);

        if (request.File != null)
        {
            var result = await SaveDoctorPhotoAsync(id, request.File);
            if (result.Result != null)
                return result.Result;

            return Ok(new { doctorId = id, fileName = result.FileName, virtualPath = result.VirtualPath, doctorPhotoUrl = BuildDoctorPhotoUrl(result.FileName) });
        }

        return Ok(new { doctorId = id });
    }

    [HttpPut("{doctorId:int}")]
    [Permission("Doctors.Manage")]
    [Consumes("application/json")]
    public async Task<IActionResult> Update([FromRoute] int doctorId, [FromBody] CrmDoctorUpsertRequest request)
    {
        request.DId = doctorId;
        var id = await _crmDoctorRepository.UpsertAsync(request);
        return Ok(new { doctorId = id });
    }

    [HttpPut("{doctorId:int}/multipart")]
    [Permission("Doctors.Manage")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Update([FromRoute] int doctorId, [FromForm] DoctorCreateFormRequest request)
    {
        request.DId = doctorId;
        var id = await _crmDoctorRepository.UpsertAsync(request);

        if (request.File != null)
        {
            var result = await SaveDoctorPhotoAsync(id, request.File);
            if (result.Result != null)
                return result.Result;

            return Ok(new { doctorId = id, fileName = result.FileName, virtualPath = result.VirtualPath, doctorPhotoUrl = BuildDoctorPhotoUrl(result.FileName) });
        }

        return Ok(new { doctorId = id });
    }

    [HttpPost("{doctorId:int}/photo")]
    [Permission("Doctors.Manage")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> UploadDoctorPhoto([FromRoute] int doctorId, [FromForm] DoctorPhotoUploadRequest request)
    {
        var file = request?.File;

        if (file == null)
            return BadRequest(new { message = "File is required" });

        if (file.Length <= 0)
            return BadRequest(new { message = "File is empty" });

        if (file.Length > MaxUploadBytes)
            return BadRequest(new { message = "File is too large. Max allowed is 5MB" });

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            return BadRequest(new { message = "Invalid file type. Allowed: jpg, jpeg, png, webp" });

        var exists = await _crmDoctorRepository.GetByIdAsync(doctorId);
        if (exists == null)
            return NotFound(new { message = $"Doctor with ID {doctorId} not found" });

        var result = await SaveDoctorPhotoAsync(doctorId, file);
        if (result.Result != null)
            return result.Result;

        return Ok(new { doctorId, fileName = result.FileName, virtualPath = result.VirtualPath, doctorPhotoUrl = BuildDoctorPhotoUrl(result.FileName) });
    }

    private async Task<(IActionResult? Result, string? FileName, string? VirtualPath)> SaveDoctorPhotoAsync(int doctorId, IFormFile file)
    {
        if (file.Length <= 0)
            return (BadRequest(new { message = "File is empty" }), null, null);

        if (file.Length > MaxUploadBytes)
            return (BadRequest(new { message = "File is too large. Max allowed is 5MB" }), null, null);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            return (BadRequest(new { message = "Invalid file type. Allowed: jpg, jpeg, png, webp" }), null, null);

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "doctors");
        Directory.CreateDirectory(root);

        string fileName;
        await using (var stream = file.OpenReadStream())
        {
            var hash = await SHA256.HashDataAsync(stream);
            var hashString = Convert.ToHexString(hash).ToLowerInvariant();
            fileName = $"doctor_{doctorId}_{hashString}{extension.ToLowerInvariant()}";
        }

        var fullPath = Path.Combine(root, fileName);

        try
        {
            await using var fs = System.IO.File.Create(fullPath);
            await file.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save doctor photo. DoctorId: {DoctorId}", doctorId);
            return (StatusCode(500, new { message = "Failed to save file" }), null, null);
        }

        var updated = await _crmDoctorRepository.UpdateDoctorPhotoAsync(doctorId, fileName);
        if (!updated)
            return (StatusCode(500, new { message = "Failed to update doctor photo name" }), null, null);

        var virtualPath = $"/images/doctors/{fileName}";
        return (null, fileName, virtualPath);
    }
}
