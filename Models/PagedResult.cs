namespace SupplierSelectionDSS.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } =
        Array.Empty<T>();

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public bool HasPreviousPage =>
        PageNumber > 1;

    public bool HasNextPage =>
        PageNumber < TotalPages;

    public int StartItemNumber =>
        TotalCount == 0
            ? 0
            : ((PageNumber - 1) * PageSize) + 1;

    public int EndItemNumber =>
        Math.Min(
            PageNumber * PageSize,
            TotalCount);
}