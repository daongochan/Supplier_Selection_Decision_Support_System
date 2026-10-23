using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;

namespace SupplierSelectionDSS.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuditLogService _auditLogService;
    private readonly ILoginAttemptService _loginAttemptService;
    private readonly ILogger<AccountController> _logger;

    // Chuỗi hash BCrypt hợp lệ (cost = 11) dùng để triệt tiêu chênh lệch thời gian khi username không tồn tại (chống timing attack)
    private const string DummyPasswordHash = "$2a$11$En7MyyM43NQeLY/ogUTCvOgkL4hJR5ciZg0DfqjByIr4U.VcJaT/W";

    public AccountController(
        ApplicationDbContext context,
        AuditLogService auditLogService,
        ILoginAttemptService loginAttemptService,
        ILogger<AccountController> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _loginAttemptService = loginAttemptService;
        _logger = logger;
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginViewModel
        {
            ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : null
        });
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("LoginLimiter")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        var effectiveReturnUrl = !string.IsNullOrWhiteSpace(model.ReturnUrl)
            ? model.ReturnUrl
            : returnUrl;
        model.ReturnUrl = !string.IsNullOrWhiteSpace(effectiveReturnUrl) && Url.IsLocalUrl(effectiveReturnUrl)
            ? effectiveReturnUrl
            : null;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var username = model.Username.Trim();
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var sanitizedUser = _loginAttemptService.SanitizeUsernameForLogging(username);

        // 1. Kiểm tra trạng thái khóa tạm thời (Lockout theo cặp IP + Username trên IMemoryCache)
        var (isLockedOut, remainingMinutes) = await _loginAttemptService.CheckLockoutAsync(username, remoteIp);
        if (isLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                $"Tài khoản tạm thời bị khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau {remainingMinutes} phút.");

            _logger.LogWarning("IP {Ip} đang trong thời gian lockout đối với tài khoản {Username}", remoteIp, sanitizedUser);
            return View(model);
        }

        var user = await _context.Users
            .SingleOrDefaultAsync(x => x.Username == username);

        // 2. Chạy xác thực mật khẩu BCrypt (luôn chạy kể cả khi user == null để đồng nhất thời gian phản hồi)
        var hashToVerify = user != null && !string.IsNullOrWhiteSpace(user.PasswordHash)
            ? user.PasswordHash
            : DummyPasswordHash;

        bool passwordValid;
        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, hashToVerify);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi kiểm tra băm mật khẩu cho tài khoản {Username}", sanitizedUser);
            passwordValid = false;
        }

        // 3. Nếu user không tồn tại hoặc mật khẩu không khớp: ghi nhận lỗi chung để tránh user enumeration
        if (user == null || !passwordValid)
        {
            var attempts = await _loginAttemptService.RecordFailedAttemptAsync(username, remoteIp);

            await _auditLogService.LogAsync(
                user?.UserId,
                AuditActions.LoginFailed,
                nameof(User),
                user?.UserId,
                description: $"Đăng nhập thất bại từ IP {remoteIp} cho tài khoản '{sanitizedUser}' (lần thử thứ {attempts}).");

            _logger.LogWarning("Đăng nhập thất bại từ IP {Ip} cho tài khoản '{Username}'. Lần sai liên tiếp: {Attempts}", remoteIp, sanitizedUser, attempts);

            ModelState.AddModelError(
                string.Empty,
                "Tên đăng nhập hoặc mật khẩu không chính xác.");

            return View(model);
        }

        // 4. Kiểm tra trạng thái tài khoản sau khi đã xác thực mật khẩu thành công
        if (!string.Equals(user.Status, EntityStatus.Active, StringComparison.OrdinalIgnoreCase))
        {
            await _auditLogService.LogAsync(
                user.UserId,
                AuditActions.LoginFailed,
                nameof(User),
                user.UserId,
                description: $"Đăng nhập bị từ chối do trạng thái tài khoản là '{user.Status}'.");

            _logger.LogWarning("Đăng nhập bị từ chối cho tài khoản '{Username}' với trạng thái {Status}", sanitizedUser, user.Status);

            ModelState.AddModelError(
                string.Empty,
                "Tài khoản hiện không hoạt động hoặc đã bị khóa. Vui lòng liên hệ quản trị viên.");

            return View(model);
        }

        // 5. Đăng nhập thành công: reset lịch sử thử sai cho cặp (IP, Username)
        await _loginAttemptService.ResetAttemptsAsync(username, remoteIp);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new(AuthClaimTypes.PasswordStamp, AuthClaimTypes.ComputePasswordStamp(user.PasswordHash))
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(7)
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(
            user.UserId,
            AuditActions.LoginSuccess,
            nameof(User),
            user.UserId,
            description: $"Đăng nhập thành công với vai trò '{user.Role}'.");

        _logger.LogInformation("Người dùng {Username} (ID {UserId}, Role {Role}) đăng nhập thành công.", user.Username, user.UserId, user.Role);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name;
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("Người dùng {Username} đã đăng xuất.", username);

        return RedirectToAction(nameof(Login));
    }

    // GET: /Account/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
