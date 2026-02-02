using Coherent.Core.DTOs;
using Coherent.Domain.Entities;

namespace Coherent.Core.Interfaces;

public interface IPatientEducationAssignmentRepository
{
    Task<(IEnumerable<PatientEducationAssignmentListDto> Assignments, int TotalCount)> GetAssignmentsAsync(
        string? mrNo,
        bool includeExpired,
        int pageNumber,
        int pageSize);
    Task<List<PatientEducationAssignmentListDto>> GetByEducationIdAsync(int educationId);
    Task<PatientEducationAssignmentDetailDto?> GetByIdAsync(int assignmentId);
    Task<TPatientEducationAssignment?> GetAssignmentAsync(int patientId, int educationId);
    Task<int> AssignAsync(PatientEducationAssignmentRequest request, int? assignedByUserId);
    Task<List<int>> BulkAssignAsync(PatientEducationBulkAssignRequest request, int? assignedByUserId);
    Task<bool> MarkAsViewedAsync(int assignmentId);
    Task<bool> UpdateAsync(int assignmentId, string? notes, string? arNotes, DateTime? expiresAt, bool? isActive);
    Task<bool> DeleteAsync(int assignmentId);
    Task<bool> DeleteByMrNoAndEducationAsync(string mrNo, int educationId);
}
