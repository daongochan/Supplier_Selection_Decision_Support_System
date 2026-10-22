using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<AccountController> _logger;

    // Chuỗi hash BCrypt giả hợp lệ (cost = 11) dùng để triệt tiêu chênh lệch thời gian khi username không tồn tại (chống timing attack)
    private const string DummyPasswordHash = "$2a$11$e87.6b8eA8K1vQc0lFz.GeP8o0W8vQWq7XwWJmI6F7m8Q9W8vQWq7";

    // Quản lý số lần thử sai theo username (chống Brute-force / Credential Stuffing)
    private static readonly ConcurrentDictionary<string, (int Attempts, DateTime LockoutEndUtc)> LoginFailures = new(StringComparer.OrdinalIgnoreCase);

    public AccountController(
        ApplicationDbContext context,
        AuditLogService auditLogService,
        ILogger<AccountController> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var username = model.Username.Trim();

        // 1. Kiểm tra trạng thái khóa tạm thời (Lockout)
        if (LoginFailures.TryGetValue(username, out var failureRecord) && failureRecord.LockoutEndUtc > DateTime.UtcNow)
        {
            var remainingMinutes = Math.Max(1, (int)Math.Ceiling((failureRecord.LockoutEndUtc - DateTime.UtcNow).TotalMinutes));
            ModelState.AddModelError(
                string.Empty,
                $"Tài khoản tạm thời bị khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau {remainingMinutes} phút.");

            _logger.LogWarning("Tài khoản {Username} đang trong thời gian lockout đến {LockoutEnd}", username, failureRecord.LockoutEndUtc);
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
            _logger.LogWarning(ex, "Lỗi kiểm tra băm mật khẩu cho tài khoản {Username}", username);
            passwordValid = false;
        }

        // 3. Nếu user không tồn tại hoặc mật khẩu không khớp: ghi nhận lỗi chung để tránh user enumeration
        if (user == null || !passwordValid)
        {
            var updated = LoginFailures.AddOrUpdate(
                username,
                _ => (1, DateTime.MinValue),
                (_, current) =>
                {
                    var count = current.Attempts + 1;
                    var lockout = count >= 5 ? DateTime.UtcNow.AddMinutes(15) : DateTime.MinValue;
                    return (count, lockout);
                });

            await _auditLogService.LogAsync(
                user?.UserId,
                AuditActions.LoginFailed,
                nameof(User),
                user?.UserId,
                description: $"Đăng nhập thất bại cho tài khoản '{username}' (lần thử thứ {updated.Attempts}).");

            _logger.LogWarning("Đăng nhập thất bại cho '{Username}'. Số lần sai liên tiếp: {Attempts}", username, updated.Attempts);

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

            _logger.LogWarning("Đăng nhập bị từ chối cho tài khoản '{Username}' với trạng thái {Status}", username, user.Status);

            ModelState.AddModelError(
                string.Empty,
                "Tài khoản hiện không hoạt động hoặc đã bị khóa. Vui lòng liên hệ quản trị viên.");

            return View(model);
        }

        // 5. Đăng nhập thành công: xóa lịch sử thử sai
        LoginFailures.TryRemove(username, out _);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, user.Role)
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
