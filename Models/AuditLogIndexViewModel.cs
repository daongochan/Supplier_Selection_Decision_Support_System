namespace SupplierSelectionDSS.Models;

public class AuditLogIndexViewModel : IPagedResult
{
    public string? Keyword { get; set; }

    public string ActionType { get; set; } = "All";

    public int? UserId { get; set; }

    public int TotalLogs { get; set; }

    public int TodayLogs { get; set; }

    public int ActiveUsersCount { get; set; }

    public int CriticalActionsCount { get; set; }

    public List<AuditLogListItemViewModel> Logs { get; set; } = new();

    public List<AuditLogUserOptionViewModel> Users { get; set; } = new();

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public int StartItemNumber => TotalCount == 0
        ? 0
        : ((PageNumber - 1) * PageSize) + 1;

    public int EndItemNumber => Math.Min(PageNumber * PageSize, TotalCount);
}
