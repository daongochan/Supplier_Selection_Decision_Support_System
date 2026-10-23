using Microsoft.EntityFrameworkCore;

namespace SupplierSelectionDSS.Models;

public class PagedResult<T> : IPagedResult
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public int StartItemNumber =>
        PaginationCalculationHelper.CalculateStartItemNumber(PageNumber, PageSize, TotalCount);

    public int EndItemNumber =>
        PaginationCalculationHelper.CalculateEndItemNumber(PageNumber, PageSize, TotalCount);

    public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> query, int pageNumber, int pageSize)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        if (totalPages > 0 && pageNumber > totalPages)
        {
            pageNumber = totalPages;
        }
        else if (totalPages == 0)
        {
            pageNumber = 1;
        }

        var skipLong = ((long)pageNumber - 1L) * pageSize;
        var skip = (int)Math.Clamp(skipLong, 0L, int.MaxValue);

        var items = totalCount == 0
            ? new List<T>()
            : await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}
