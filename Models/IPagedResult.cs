namespace SupplierSelectionDSS.Models;

/// <summary>
/// Giao diện chuẩn cho các kết quả hoặc ViewModel có hỗ trợ phân trang.
/// </summary>
public interface IPagedResult
{
    int PageNumber { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    bool HasPreviousPage { get; }
    bool HasNextPage { get; }
    int StartItemNumber { get; }
    int EndItemNumber { get; }
}

public static class PaginationCalculationHelper
{
    public static int CalculateStartItemNumber(int pageNumber, int pageSize, int totalCount)
    {
        if (totalCount <= 0)
        {
            return 0;
        }

        var safePage = Math.Max(1L, pageNumber);
        var safeSize = Math.Clamp((long)pageSize, 1L, 100L);
        var start = ((safePage - 1L) * safeSize) + 1L;
        return start > totalCount ? 0 : (int)Math.Min(start, int.MaxValue);
    }

    public static int CalculateEndItemNumber(int pageNumber, int pageSize, int totalCount)
    {
        if (totalCount <= 0)
        {
            return 0;
        }

        var safePage = Math.Max(1L, pageNumber);
        var safeSize = Math.Clamp((long)pageSize, 1L, 100L);
        var start = ((safePage - 1L) * safeSize) + 1L;
        if (start > totalCount)
        {
            return 0;
        }

        var end = Math.Min(safePage * safeSize, totalCount);
        return (int)Math.Clamp(end, 0L, int.MaxValue);
    }
}
