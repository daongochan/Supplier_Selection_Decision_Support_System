namespace SupplierSelectionDSS.Exceptions;

/// <summary>
/// Ngoại lệ đại diện cho các vi phạm quy tắc nghiệp vụ cần thông báo trực tiếp đến người dùng.
/// Kế thừa InvalidOperationException để đảm bảo tính tương thích với các bộ kiểm thử và khối xử lý hiện có.
/// </summary>
public class BusinessRuleException : InvalidOperationException
{
    public BusinessRuleException()
    {
    }

    public BusinessRuleException(string? message) : base(message)
    {
    }

    public BusinessRuleException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
