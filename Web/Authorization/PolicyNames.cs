namespace SupplierSelectionDSS.Web.Authorization;

/// <summary>
/// Định nghĩa tên các Authorization Policy dùng trong ứng dụng.
/// Thay thế cho việc hardcode các chuỗi Role trong [Authorize(Roles = "...")].
/// </summary>
public static class PolicyNames
{
    public const string ManageUsers = "ManageUsers";
    public const string ManageBatches = "ManageBatches";
    public const string EvaluateSuppliers = "EvaluateSuppliers";
    public const string PerformAHP = "PerformAHP";
    public const string PerformTOPSIS = "PerformTOPSIS";
    public const string ApproveDecision = "ApproveDecision";
    public const string ViewReports = "ViewReports";
    public const string ExportReports = "ExportReports";
}
