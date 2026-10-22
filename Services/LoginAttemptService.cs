using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SupplierSelectionDSS.Services;

public class LoginAttemptService : ILoginAttemptService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<LoginAttemptService> _logger;

    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);

    public LoginAttemptService(IMemoryCache cache, ILogger<LoginAttemptService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<(bool IsLockedOut, int RemainingMinutes)> CheckLockoutAsync(string username, string ipAddress)
    {
        var lockoutKey = GetLockoutKey(username, ipAddress);
        if (_cache.TryGetValue(lockoutKey, out DateTime lockoutUntilUtc))
        {
            if (lockoutUntilUtc > DateTime.UtcNow)
            {
                var remaining = (int)Math.Ceiling((lockoutUntilUtc - DateTime.UtcNow).TotalMinutes);
                return Task.FromResult((true, Math.Max(1, remaining)));
            }
            // Expired, clear it
            _cache.Remove(lockoutKey);
        }

        return Task.FromResult((false, 0));
    }

    public Task<int> RecordFailedAttemptAsync(string username, string ipAddress)
    {
        var attemptsKey = GetAttemptsKey(username, ipAddress);
        var lockoutKey = GetLockoutKey(username, ipAddress);

        var currentAttempts = _cache.GetOrCreate(attemptsKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = FailureWindow;
            entry.Size = 1;
            return 0;
        });

        currentAttempts++;

        _cache.Set(attemptsKey, currentAttempts, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = FailureWindow,
            Size = 1
        });

        if (currentAttempts >= MaxFailedAttempts)
        {
            var lockoutUntil = DateTime.UtcNow.Add(LockoutDuration);
            _cache.Set(lockoutKey, lockoutUntil, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = LockoutDuration,
                Size = 1
            });

            // Reset failure counter when lockout is triggered so that after lockout expires,
            // the user is NOT immediately locked again after 1 single mistake!
            _cache.Remove(attemptsKey);

            _logger.LogWarning(
                "IP {IpAddress} với tài khoản {Username} đã bị khóa tạm thời 15 phút do thử sai {Attempts} lần liên tiếp.",
                SanitizeIp(ipAddress),
                SanitizeUsernameForLogging(username),
                currentAttempts);
        }

        return Task.FromResult(currentAttempts);
    }

    public Task ResetAttemptsAsync(string username, string ipAddress)
    {
        _cache.Remove(GetAttemptsKey(username, ipAddress));
        _cache.Remove(GetLockoutKey(username, ipAddress));
        return Task.CompletedTask;
    }

    public string SanitizeUsernameForLogging(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "[Empty]";

        // Loại bỏ ký tự điều khiển (tránh CRLF injection / log forging)
        var cleaned = new string(username.Where(c => !char.IsControl(c)).ToArray()).Trim();

        // Giới hạn độ dài tối đa 50 ký tự
        if (cleaned.Length > 50)
        {
            cleaned = cleaned.Substring(0, 50) + "...";
        }

        return cleaned;
    }

    private static string GetAttemptsKey(string username, string ipAddress) =>
        $"login_attempts:{SanitizeIp(ipAddress)}:{NormalizeUsername(username)}";

    private static string GetLockoutKey(string username, string ipAddress) =>
        $"login_lockout:{SanitizeIp(ipAddress)}:{NormalizeUsername(username)}";

    private static string NormalizeUsername(string username) =>
        (username ?? string.Empty).Trim().ToLowerInvariant();

    private static string SanitizeIp(string ipAddress) =>
        string.IsNullOrWhiteSpace(ipAddress) ? "unknown_ip" : ipAddress.Trim();
}
