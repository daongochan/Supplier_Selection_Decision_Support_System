using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SupplierSelectionDSS.Services;
using Xunit;

namespace SupplierSelectionDSS.Tests.Services;

public class LoginAttemptServiceTests
{
    private readonly LoginAttemptService _service;
    private readonly IMemoryCache _cache;

    public LoginAttemptServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
        _service = new LoginAttemptService(_cache, NullLogger<LoginAttemptService>.Instance);
    }

    [Fact]
    public async Task CheckLockout_Initially_ReturnsNotLockedOut()
    {
        var result = await _service.CheckLockoutAsync("admin", "192.168.1.100");

        result.IsLockedOut.Should().BeFalse();
        result.RemainingMinutes.Should().Be(0);
    }

    [Fact]
    public async Task RecordFailedAttempt_IncrementsCountUntilThreshold()
    {
        const string user = "staff01";
        const string ip = "10.0.0.1";

        var attempt1 = await _service.RecordFailedAttemptAsync(user, ip);
        var attempt2 = await _service.RecordFailedAttemptAsync(user, ip);
        var attempt3 = await _service.RecordFailedAttemptAsync(user, ip);
        var attempt4 = await _service.RecordFailedAttemptAsync(user, ip);

        attempt1.Should().Be(1);
        attempt2.Should().Be(2);
        attempt3.Should().Be(3);
        attempt4.Should().Be(4);

        var (isLocked, _) = await _service.CheckLockoutAsync(user, ip);
        isLocked.Should().BeFalse();
    }

    [Fact]
    public async Task FifthFailedAttempt_TriggersLockoutForThatIpAndUser()
    {
        const string user = "admin";
        const string attackerIp = "203.0.113.45";

        for (int i = 0; i < 5; i++)
        {
            await _service.RecordFailedAttemptAsync(user, attackerIp);
        }

        var (isLocked, remaining) = await _service.CheckLockoutAsync(user, attackerIp);
        isLocked.Should().BeTrue();
        remaining.Should().BeInRange(1, 15);
    }

    [Fact]
    public async Task AttackerLockout_DoesNotLockAdminOnLegitimateIp()
    {
        const string user = "admin";
        const string attackerIp = "198.51.100.99";
        const string legitimateOfficeIp = "192.168.1.50";

        // Attacker enters wrong password 5 times
        for (int i = 0; i < 5; i++)
        {
            await _service.RecordFailedAttemptAsync(user, attackerIp);
        }

        // Attacker is locked out
        var attackerStatus = await _service.CheckLockoutAsync(user, attackerIp);
        attackerStatus.IsLockedOut.Should().BeTrue();

        // Admin from legitimate IP is NOT locked out
        var adminStatus = await _service.CheckLockoutAsync(user, legitimateOfficeIp);
        adminStatus.IsLockedOut.Should().BeFalse();
    }

    [Fact]
    public async Task ResetAttempts_ClearsFailuresAndLockout()
    {
        const string user = "expert01";
        const string ip = "172.16.0.20";

        for (int i = 0; i < 5; i++)
        {
            await _service.RecordFailedAttemptAsync(user, ip);
        }

        var beforeReset = await _service.CheckLockoutAsync(user, ip);
        beforeReset.IsLockedOut.Should().BeTrue();

        await _service.ResetAttemptsAsync(user, ip);

        var afterReset = await _service.CheckLockoutAsync(user, ip);
        afterReset.IsLockedOut.Should().BeFalse();
    }

    [Theory]
    [InlineData("normaluser", "normaluser")]
    [InlineData("admin\r\nInjected: LogHeader", "adminInjected: LogHeader")]
    [InlineData("   padded   ", "padded")]
    [InlineData("", "[Empty]")]
    [InlineData(null, "[Empty]")]
    public void SanitizeUsernameForLogging_StripsControlCharsAndHandlesEdgeCases(string? input, string expected)
    {
        var sanitized = _service.SanitizeUsernameForLogging(input);
        sanitized.Should().Be(expected);
    }

    [Fact]
    public void SanitizeUsernameForLogging_ClampsLongStrings()
    {
        var veryLong = new string('A', 80);
        var sanitized = _service.SanitizeUsernameForLogging(veryLong);

        sanitized.Length.Should().Be(53); // 50 chars + "..."
        sanitized.Should().EndWith("...");
    }
}
