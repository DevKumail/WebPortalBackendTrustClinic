using Coherent.Core.DTOs;

namespace Coherent.Core.Interfaces;

public interface IMobileUserRepository
{
    Task<(IEnumerable<MobileUserListItemDto> Users, int TotalCount)> SearchMobileUsersAsync(
        string? mrNo,
        int pageNumber,
        int pageSize);
}
