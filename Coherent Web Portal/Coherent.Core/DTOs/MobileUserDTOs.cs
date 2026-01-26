namespace Coherent.Core.DTOs;

public class MobileUserListItemDto
{
    public int Id { get; set; }
    public string? MRNo { get; set; }
    public string? FullName { get; set; }
}

public class MobileUserSearchRequest
{
    public string? MRNo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PaginatedMobileUserResponse
{
    public List<MobileUserListItemDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}
