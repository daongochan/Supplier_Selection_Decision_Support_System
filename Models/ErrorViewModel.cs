namespace SupplierSelectionDSS.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public int StatusCode { get; set; } = 500;

    public string Title { get; set; } = "Lỗi xử lý hệ thống";

    public string Message { get; set; } = "Đã xảy ra sự cố trong quá trình xử lý yêu cầu của bạn.";

    public string? Description { get; set; }
}
