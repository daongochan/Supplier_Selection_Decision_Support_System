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
