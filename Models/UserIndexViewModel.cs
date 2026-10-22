namespace SupplierSelectionDSS.Models;

public class UserIndexViewModel : IPagedResult
{
    public string? Keyword { get; set; }

    public string Role { get; set; } = "All";

    public string Status { get; set; } = "All";

    public List<UserListItemViewModel> Users { get; set; } = new();

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public int StartItemNumber => TotalCount == 0
        ? 0
        : ((PageNumber - 1) * PageSize) + 1;

    public int EndItemNumber => Math.Min(PageNumber * PageSize, TotalCount);
}


public class UserListItemViewModel
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
}