using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class PatientEducationAssignmentRepository : IPatientEducationAssignmentRepository
{
    private readonly IDbConnection _connection;

    public PatientEducationAssignmentRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<PatientEducationAssignmentListDto>> GetByMrNoAsync(string mrNo, bool includeExpired = false)
    {
        var patientId = await ResolvePatientIdByMrNoAsync(mrNo);
        if (patientId == null)
            return new List<PatientEducationAssignmentListDto>();

        var sql = @"
SELECT
    a.AssignmentId,
    a.EducationId,
    e.Title AS EducationTitle,
    e.ArTitle AS ArEducationTitle,
    c.CategoryName,
    u.FullName AS PatientName,
    u.MRNO AS PatientMRN,
    a.AssignedByUserId,
    a.AssignedAt,
    a.Notes,
    a.ArNotes,
    a.IsViewed,
    a.ViewedAt,
    a.ExpiresAt,
    a.IsActive
FROM TPatientEducationAssignment a
LEFT JOIN dbo.Users u ON u.Id = a.PatientId AND u.IsDeleted = 0
INNER JOIN MPatientEducation e ON a.EducationId = e.EducationId
INNER JOIN MPatientEducationCategory c ON e.CategoryId = c.CategoryId
WHERE a.PatientId = @PatientId 
  AND a.IsDeleted = 0
  AND a.IsActive = 1
  AND e.IsDeleted = 0
  AND e.Active = 1
  AND (@IncludeExpired = 1 OR a.ExpiresAt IS NULL OR a.ExpiresAt > GETDATE())
ORDER BY a.AssignedAt DESC";

        var rows = await _connection.QueryAsync<PatientEducationAssignmentListDto>(sql, new
        {
            PatientId = patientId.Value,
            IncludeExpired = includeExpired ? 1 : 0
        });
        return rows.ToList();
    }

    public async Task<List<PatientEducationAssignmentListDto>> GetByEducationIdAsync(int educationId)
    {
        var sql = @"
SELECT
    a.AssignmentId,
    a.EducationId,
    e.Title AS EducationTitle,
    e.ArTitle AS ArEducationTitle,
    c.CategoryName,
    u.FullName AS PatientName,
    u.MRNO AS PatientMRN,
    a.AssignedByUserId,
    a.AssignedAt,
    a.Notes,
    a.ArNotes,
    a.IsViewed,
    a.ViewedAt,
    a.ExpiresAt,
    a.IsActive
FROM TPatientEducationAssignment a
LEFT JOIN dbo.Users u ON u.Id = a.PatientId AND u.IsDeleted = 0
INNER JOIN MPatientEducation e ON a.EducationId = e.EducationId
INNER JOIN MPatientEducationCategory c ON e.CategoryId = c.CategoryId
WHERE a.EducationId = @EducationId 
  AND a.IsDeleted = 0
ORDER BY a.AssignedAt DESC";

        var rows = await _connection.QueryAsync<PatientEducationAssignmentListDto>(sql, new { EducationId = educationId });
        return rows.ToList();
    }

    public async Task<PatientEducationAssignmentDetailDto?> GetByIdAsync(int assignmentId)
    {
        var sql = @"
SELECT
    a.AssignmentId,
    a.EducationId,
    e.Title AS EducationTitle,
    e.ArTitle AS ArEducationTitle,
    c.CategoryName,
    u.FullName AS PatientName,
    u.MRNO AS PatientMRN,
    a.AssignedByUserId,
    a.AssignedAt,
    a.Notes,
    a.ArNotes,
    a.IsViewed,
    a.ViewedAt,
    a.ExpiresAt,
    a.IsActive
FROM TPatientEducationAssignment a
LEFT JOIN dbo.Users u ON u.Id = a.PatientId AND u.IsDeleted = 0
INNER JOIN MPatientEducation e ON a.EducationId = e.EducationId
INNER JOIN MPatientEducationCategory c ON e.CategoryId = c.CategoryId
WHERE a.AssignmentId = @AssignmentId AND a.IsDeleted = 0";

        return await _connection.QueryFirstOrDefaultAsync<PatientEducationAssignmentDetailDto>(sql, new { AssignmentId = assignmentId });
    }

    public async Task<TPatientEducationAssignment?> GetAssignmentAsync(int patientId, int educationId)
    {
        var sql = @"
SELECT * FROM TPatientEducationAssignment 
WHERE PatientId = @PatientId AND EducationId = @EducationId AND IsDeleted = 0";

        return await _connection.QueryFirstOrDefaultAsync<TPatientEducationAssignment>(sql, new { PatientId = patientId, EducationId = educationId });
    }

    public async Task<int> AssignAsync(PatientEducationAssignmentRequest request, int? assignedByUserId)
    {
        var patientId = await ResolvePatientIdByMrNoAsync(request.MRNo);
        if (patientId == null)
            throw new InvalidOperationException($"Patient not found for MRNo {request.MRNo}");

        // Check if already assigned
        var existing = await GetAssignmentAsync(patientId.Value, request.EducationId);
        if (existing != null)
        {
            // Reactivate if soft-deleted or inactive
            var updateSql = @"
UPDATE TPatientEducationAssignment
SET IsActive = 1,
    IsDeleted = 0,
    Notes = @Notes,
    ArNotes = @ArNotes,
    ExpiresAt = @ExpiresAt,
    AssignedByUserId = @AssignedByUserId,
    AssignedAt = GETDATE(),
    UpdatedAt = GETDATE()
WHERE AssignmentId = @AssignmentId";

            await _connection.ExecuteAsync(updateSql, new 
            { 
                existing.AssignmentId,
                request.Notes,
                request.ArNotes,
                request.ExpiresAt,
                AssignedByUserId = assignedByUserId
            });
            return existing.AssignmentId;
        }

        var insertSql = @"
INSERT INTO TPatientEducationAssignment
(PatientId, EducationId, AssignedByUserId, AssignedAt, Notes, ArNotes, ExpiresAt, IsViewed, IsActive, IsDeleted, CreatedAt)
VALUES
(@PatientId, @EducationId, @AssignedByUserId, GETDATE(), @Notes, @ArNotes, @ExpiresAt, 0, 1, 0, GETDATE());
SELECT CAST(SCOPE_IDENTITY() as int);";

        return await _connection.QuerySingleAsync<int>(insertSql, new 
        { 
            PatientId = patientId.Value,
            request.EducationId,
            AssignedByUserId = assignedByUserId,
            request.Notes,
            request.ArNotes,
            request.ExpiresAt
        });
    }

    public async Task<List<int>> BulkAssignAsync(PatientEducationBulkAssignRequest request, int? assignedByUserId)
    {
        var assignmentIds = new List<int>();
        
        foreach (var mrNo in request.MRNos)
        {
            var singleRequest = new PatientEducationAssignmentRequest
            {
                MRNo = mrNo,
                EducationId = request.EducationId,
                Notes = request.Notes,
                ArNotes = request.ArNotes,
                ExpiresAt = request.ExpiresAt
            };
            
            var id = await AssignAsync(singleRequest, assignedByUserId);
            assignmentIds.Add(id);
        }
        
        return assignmentIds;
    }

    public async Task<bool> MarkAsViewedAsync(int assignmentId)
    {
        var sql = @"
UPDATE TPatientEducationAssignment
SET IsViewed = 1,
    ViewedAt = GETDATE(),
    UpdatedAt = GETDATE()
WHERE AssignmentId = @AssignmentId AND IsDeleted = 0";

        var rows = await _connection.ExecuteAsync(sql, new { AssignmentId = assignmentId });
        return rows > 0;
    }

    public async Task<bool> UpdateAsync(int assignmentId, string? notes, string? arNotes, DateTime? expiresAt, bool? isActive)
    {
        var sql = @"
UPDATE TPatientEducationAssignment
SET Notes = COALESCE(@Notes, Notes),
    ArNotes = COALESCE(@ArNotes, ArNotes),
    ExpiresAt = @ExpiresAt,
    IsActive = COALESCE(@IsActive, IsActive),
    UpdatedAt = GETDATE()
WHERE AssignmentId = @AssignmentId AND IsDeleted = 0";

        var rows = await _connection.ExecuteAsync(sql, new 
        { 
            AssignmentId = assignmentId,
            Notes = notes,
            ArNotes = arNotes,
            ExpiresAt = expiresAt,
            IsActive = isActive
        });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int assignmentId)
    {
        var sql = @"
UPDATE TPatientEducationAssignment 
SET IsDeleted = 1, UpdatedAt = GETDATE() 
WHERE AssignmentId = @AssignmentId";
        
        var rows = await _connection.ExecuteAsync(sql, new { AssignmentId = assignmentId });
        return rows > 0;
    }

    public async Task<bool> DeleteByMrNoAndEducationAsync(string mrNo, int educationId)
    {
        var patientId = await ResolvePatientIdByMrNoAsync(mrNo);
        if (patientId == null)
            return false;

        var sql = @"
UPDATE TPatientEducationAssignment 
SET IsDeleted = 1, UpdatedAt = GETDATE() 
WHERE PatientId = @PatientId AND EducationId = @EducationId AND IsDeleted = 0";
        
        var rows = await _connection.ExecuteAsync(sql, new { PatientId = patientId.Value, EducationId = educationId });
        return rows > 0;
    }

    private async Task<int?> ResolvePatientIdByMrNoAsync(string? mrNo)
    {
        if (string.IsNullOrWhiteSpace(mrNo))
            return null;

        var sql = @"SELECT TOP 1 Id FROM dbo.Users WHERE MRNO = @MRNO AND IsDeleted = 0 ORDER BY Id DESC";
        return await _connection.QueryFirstOrDefaultAsync<int?>(sql, new { MRNO = mrNo.Trim() });
    }
}
