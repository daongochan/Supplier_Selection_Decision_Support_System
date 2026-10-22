using System.Security.Claims;

namespace SupplierSelectionDSS.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Trích xuất UserId từ Claims của User hiện tại.
    /// Trả về null nếu không tìm thấy claim NameIdentifier hoặc không thể parse thành số nguyên hợp lệ > 0.
    /// </summary>
    public static int? GetUserId(this ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            return null;
        }

        var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var userId) || userId <= 0)
        {
            return null;
        }

        return userId;
    }

    /// <summary>
    /// Trích xuất tên hiển thị (GivenName hoặc Username) của User hiện tại.
    /// </summary>
    public static string GetDisplayName(this ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            return "User";
        }

        return principal.FindFirst(ClaimTypes.GivenName)?.Value
            ?? principal.Identity?.Name
            ?? "User";
    }

    /// <summary>
    /// Trích xuất vai trò (Role) của User hiện tại.
    /// </summary>
    public static string GetRole(this ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            return string.Empty;
        }

        return principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
