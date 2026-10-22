namespace SupplierSelectionDSS.Constants;

public static class UserRole
{
    public const string Administrator = "Administrator";
    public const string PurchasingStaff = "PurchasingStaff";
    public const string Expert = "Expert";
    public const string Manager = "Manager";

    public static readonly IReadOnlyList<string> All =
    [
        Administrator,
        PurchasingStaff,
        Expert,
        Manager
    ];
}
