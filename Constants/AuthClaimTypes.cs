using System.Security.Cryptography;
using System.Text;

namespace SupplierSelectionDSS.Constants;

public static class AuthClaimTypes
{
    public const string PasswordStamp = "dss:pwd_stamp";

    public static string ComputePasswordStamp(string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return string.Empty;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash));
        return Convert.ToHexString(bytes.AsSpan(0, 16));
    }
}

public record AuthUserValidationSnapshot(
    int UserId,
    string Username,
    string FullName,
    string Role,
    string Status,
    string PasswordHash);
