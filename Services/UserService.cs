using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Services;

public class UserService
{
    private readonly ApplicationDbContext _context;


    private static readonly string[] AllowedRoles =
    {
        UserRole.Administrator,
        UserRole.PurchasingStaff,
        UserRole.Expert,
        UserRole.Manager,
    };


    private static readonly string[] AllowedStatuses =
    {
        EntityStatus.Active,
        EntityStatus.Locked,
        EntityStatus.Inactive
    };


    public UserService(
        ApplicationDbContext context)
    {
        _context = context;
    }


    // =========================================================
    // SEARCH
    // =========================================================

    public async Task<PagedResult<UserListItemViewModel>> SearchAsync(
        string? keyword,
        string? role,
        string? status,
        int pageNumber = 1,
        int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Max(pageSize, 1);

        keyword =
            keyword?.Trim();


        role =
            string.IsNullOrWhiteSpace(role)
                ? "All"
                : role.Trim();


        status =
            string.IsNullOrWhiteSpace(status)
                ? "All"
                : status.Trim();


        var query =
            _context.Users
                .AsNoTracking()
                .AsQueryable();


        // =====================================================
        // KEYWORD
        // =====================================================

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query =
                query.Where(
                    x =>
                        x.Username.Contains(keyword) ||
                        x.FullName.Contains(keyword) ||
                        (x.Email != null &&
                         x.Email.Contains(keyword)));
        }


        // =====================================================
        // ROLE
        // =====================================================

        if (
            role != "All" &&
            AllowedRoles.Contains(role))
        {
            query =
                query.Where(
                    x =>
                        x.Role ==
                        role);
        }


        // =====================================================
        // STATUS
        // =====================================================

        if (
            status != "All" &&
            AllowedStatuses.Contains(status))
        {
            query =
                query.Where(
                    x =>
                        x.Status ==
                        status);
        }


        var projectedQuery = query
            .OrderBy(
                x =>
                    x.FullName)
            .ThenBy(
                x =>
                    x.Username)
            .Select(
                x =>
                    new UserListItemViewModel
                    {
                        UserId =
                            x.UserId,

                        Username =
                            x.Username,

                        FullName =
                            x.FullName,

                        Email =
                            x.Email,

                        Phone =
                            x.Phone,

                        Role =
                            x.Role,

                        Status =
                            x.Status,

                        LastLoginAt =
                            x.LastLoginAt,

                        CreatedAt =
                            x.CreatedAt
                    });

        return await PagedResult<UserListItemViewModel>.CreateAsync(projectedQuery, pageNumber, pageSize);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<User?>
        GetByIdAsync(
            int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(
                x =>
                    x.UserId ==
                    id);
    }


    // =========================================================
    // USERNAME EXISTS
    // =========================================================

    public async Task<bool>
        UsernameExistsAsync(
            string username,
            int? excludeUserId = null)
    {
        username =
            username.Trim();


        if (
            string.IsNullOrWhiteSpace(
                username))
        {
            return false;
        }


        var query =
            _context.Users
                .Where(
                    x =>
                        x.Username ==
                        username);


        if (excludeUserId.HasValue)
        {
            query =
                query.Where(
                    x =>
                        x.UserId !=
                        excludeUserId.Value);
        }


        return await query.AnyAsync();
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<int> CreateAsync(
        UserFormViewModel model)
    {
        ValidateRole(
            model.Role);


        ValidateStatus(
            model.Status);


        if (
            string.IsNullOrWhiteSpace(
                model.Password))
        {
            throw new InvalidOperationException(
                "Mật khẩu là bắt buộc khi tạo tài khoản.");
        }


        var username =
            model.Username.Trim();


        if (
            await UsernameExistsAsync(
                username))
        {
            throw new InvalidOperationException(
                "Username đã tồn tại.");
        }


        var user =
            new User
            {
                Username =
                    username,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        model.Password),

                FullName =
                    model.FullName.Trim(),

                Email =
                    NormalizeNullable(
                        model.Email),

                Phone =
                    NormalizeNullable(
                        model.Phone),

                Role =
                    model.Role,

                Status =
                    model.Status,

                CreatedAt =
                    DateTime.UtcNow,

                UpdatedAt =
                    DateTime.UtcNow
            };


        _context.Users.Add(
            user);


        await _context.SaveChangesAsync();


        return user.UserId;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task UpdateAsync(
        UserFormViewModel model,
        int currentUserId)
    {
        ValidateRole(
            model.Role);


        ValidateStatus(
            model.Status);


        var user =
            await _context.Users
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId ==
                        model.UserId);


        if (user == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy người dùng.");
        }


        // =====================================================
        // USERNAME
        // =====================================================

        var username =
            model.Username.Trim();


        if (
            await UsernameExistsAsync(
                username,
                model.UserId))
        {
            throw new InvalidOperationException(
                "Username đã tồn tại.");
        }


        // Username được giữ nguyên về mặt nghiệp vụ.
        user.Username =
            user.Username.Trim();


        // =====================================================
        // SELF STATUS PROTECTION
        // =====================================================

        if (
            user.UserId ==
            currentUserId &&
            !string.Equals(
                model.Status,
                EntityStatus.Active,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Bạn không thể khóa hoặc vô hiệu hóa chính tài khoản đang đăng nhập.");
        }


        // =====================================================
        // LAST ADMIN PROTECTION
        // =====================================================

        if (
            user.Role == UserRole.Administrator &&
            model.Status != EntityStatus.Active)
        {
            var activeAdminCount =
                await _context.Users
                    .CountAsync(
                        x =>
                            x.Role ==
                                UserRole.Administrator
                            &&
                            x.Status ==
                                EntityStatus.Active);


            if (activeAdminCount <= 1)
            {
                throw new InvalidOperationException(
                    "Không thể vô hiệu hóa Administrator cuối cùng.");
            }
        }


        // =====================================================
        // UPDATE
        // =====================================================

        user.FullName =
            model.FullName.Trim();


        user.Email =
            NormalizeNullable(
                model.Email);


        user.Phone =
            NormalizeNullable(
                model.Phone);


        user.Role =
            model.Role;


        user.Status =
            model.Status;


        // Password chỉ thay đổi khi người dùng nhập mới.

        if (
            !string.IsNullOrWhiteSpace(
                model.Password))
        {
            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    model.Password);
        }


        user.UpdatedAt =
            DateTime.UtcNow;


        await _context.SaveChangesAsync();
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private static void ValidateRole(
        string role)
    {
        if (
            !AllowedRoles.Contains(
                role))
        {
            throw new InvalidOperationException(
                "Vai trò người dùng không hợp lệ.");
        }
    }


    private static void ValidateStatus(
        string status)
    {
        if (
            !AllowedStatuses.Contains(
                status))
        {
            throw new InvalidOperationException(
                "Trạng thái tài khoản không hợp lệ.");
        }
    }


    private static string? NormalizeNullable(
        string? value)
    {
        var normalized =
            value?.Trim();


        return string.IsNullOrWhiteSpace(
            normalized)
                ? null
                : normalized;
    }
}