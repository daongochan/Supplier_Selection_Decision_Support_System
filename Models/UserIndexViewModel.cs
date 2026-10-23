namespace SupplierSelectionDSS.Models;

public class UserIndexViewModel : IPagedResult
{
    public string? Keyword { get; set; }

    public string Role { get; set; } = "All";

    public string Status { get; set; } = "All";

    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int LockedUsers { get; set; }

    public int InactiveUsers { get; set; }

    public List<UserListItemViewModel> Users { get; set; } = new();

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public int StartItemNumber =>
        PaginationCalculationHelper.CalculateStartItemNumber(PageNumber, PageSize, TotalCount);

    public int EndItemNumber =>
        PaginationCalculationHelper.CalculateEndItemNumber(PageNumber, PageSize, TotalCount);
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
