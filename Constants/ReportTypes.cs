namespace SupplierSelectionDSS.Constants;

/// <summary>
/// Các loại định dạng báo cáo được hỗ trợ trong hệ thống.
/// </summary>
public static class ReportTypes
{
    public const string Pdf = "PDF";
    public const string Excel = "Excel";

    public static readonly string[] All = { Pdf, Excel };
}
