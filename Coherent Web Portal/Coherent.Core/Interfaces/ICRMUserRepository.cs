using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

public interface ICRMUserRepository
{
    Task<CRMUserListResponse> GetCRMUsersAsync(int? empType, bool? isCRM, int limit);
    Task<UpdateIsCRMResponse> UpdateIsCRMAsync(long empId, bool isCRM);
    Task<UpdateIsCRMResponse> BulkUpdateIsCRMAsync(List<long> empIds, bool isCRM);
    Task<UpdateRoleIdResponse> UpdateRoleIdAsync(long empId, int? roleId);
}
