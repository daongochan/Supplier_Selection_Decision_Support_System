namespace SupplierSelectionDSS.Services;

public interface ILoginAttemptService
{
    Task<(bool IsLockedOut, int RemainingMinutes)> CheckLockoutAsync(string username, string ipAddress);
    Task<int> RecordFailedAttemptAsync(string username, string ipAddress);
    Task ResetAttemptsAsync(string username, string ipAddress);
    string SanitizeUsernameForLogging(string? username);
}
