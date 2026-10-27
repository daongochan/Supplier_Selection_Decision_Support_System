namespace SupplierSelectionDSS.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public int StatusCode { get; set; } = 500;

    public string Title { get; set; } = "System Error";

    public string Message { get; set; } = "An error occurred while processing your request.";

    public string? Description { get; set; }
}
