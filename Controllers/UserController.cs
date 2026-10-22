using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Web.Extensions;

namespace SupplierSelectionDSS.Controllers;

[Authorize(Roles = UserRole.Administrator)]
public class UserController : Controller
{
    private readonly UserService _userService;

    private readonly AuditLogService _auditLogService;


    public UserController(
        UserService userService,
        AuditLogService auditLogService)
    {
        _userService =
            userService;

        _auditLogService =
            auditLogService;
    }


    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword,
        string role = "All",
        string status = "All",
        int page = 1,
        int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Max(pageSize, 1);

        var paged = await _userService.SearchAsync(
            keyword,
            role,
            status,
            page,
            pageSize);

        var model =
            new UserIndexViewModel
            {
                Keyword =
                    keyword,

                Role =
                    role,

                Status =
                    status,

                Users =
                    paged.Items.ToList(),

                PageNumber =
                    paged.PageNumber,

                PageSize =
                    paged.PageSize,

                TotalCount =
                    paged.TotalCount,

                TotalPages =
                    paged.TotalPages
            };


        return View(model);
    }


    // =========================================================
    // CREATE - GET
    // =========================================================

    [HttpGet]
    public IActionResult Create()
    {
        return View(
            new UserFormViewModel());
    }


    // =========================================================
    // CREATE - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        UserFormViewModel model)
    {
        Normalize(model);


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        var currentUserId =
            User.GetUserId();


        if (!currentUserId.HasValue)
        {
            return Unauthorized();
        }


        try
        {
            var newUserId =
                await _userService
                    .CreateAsync(
                        model);


            await _auditLogService
                .LogAsync(
                    currentUserId.Value,
                    AuditActions.CreateUser,
                    "Users",
                    newUserId,
                    null,
                    JsonSerializer.Serialize(
                        new
                        {
                            model.Username,
                            model.FullName,
                            model.Email,
                            model.Phone,
                            model.Role,
                            model.Status
                        }),
                    $"Tạo tài khoản người dùng '{model.Username}'.");


            TempData["SuccessMessage"] =
                "Tài khoản đã được tạo thành công.";


            return RedirectToAction(
                nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);


            return View(model);
        }
    }


    // =========================================================
    // EDIT - GET
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id)
    {
        var user =
            await _userService
                .GetByIdAsync(id);


        if (user == null)
        {
            return NotFound();
        }


        var model =
            new UserFormViewModel
            {
                UserId =
                    user.UserId,

                Username =
                    user.Username,

                FullName =
                    user.FullName,

                Email =
                    user.Email,

                Phone =
                    user.Phone,

                Role =
                    user.Role,

                Status =
                    user.Status
            };


        return View(model);
    }


    // =========================================================
    // EDIT - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        UserFormViewModel model)
    {
        Normalize(model);


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        var currentUserId =
            User.GetUserId();


        if (!currentUserId.HasValue)
        {
            return Unauthorized();
        }


        // =====================================================
        // LOAD CURRENT USER DATA
        // =====================================================

        var existingUser =
            await _userService
                .GetByIdAsync(
                    model.UserId);


        if (existingUser == null)
        {
            return NotFound();
        }


        var oldValue =
            JsonSerializer.Serialize(
                new
                {
                    existingUser.Username,
                    existingUser.FullName,
                    existingUser.Email,
                    existingUser.Phone,
                    existingUser.Role,
                    existingUser.Status
                });


        try
        {
            await _userService
                .UpdateAsync(
                    model,
                    currentUserId.Value);


            // =================================================
            // LOAD UPDATED DATA
            // =================================================

            var updatedUser =
                await _userService
                    .GetByIdAsync(
                        model.UserId);


            if (updatedUser == null)
            {
                throw new InvalidOperationException(
                    "Không thể tải lại thông tin tài khoản sau khi cập nhật.");
            }


            var newValue =
                JsonSerializer.Serialize(
                    new
                    {
                        updatedUser.Username,
                        updatedUser.FullName,
                        updatedUser.Email,
                        updatedUser.Phone,
                        updatedUser.Role,
                        updatedUser.Status
                    });


            await _auditLogService
                .LogAsync(
                    currentUserId.Value,
                    AuditActions.UpdateUser,
                    "Users",
                    updatedUser.UserId,
                    oldValue,
                    newValue,
                    $"Cập nhật tài khoản người dùng '{updatedUser.Username}'.");


            TempData["SuccessMessage"] =
                "Thông tin tài khoản đã được cập nhật.";


            return RedirectToAction(
                nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);


            return View(model);
        }
    }


    // =========================================================
    // SUPPORT
    // =========================================================


    private static void Normalize(
        UserFormViewModel model)
    {
        model.Username =
            model.Username.Trim();


        model.FullName =
            model.FullName.Trim();


        model.Email =
            model.Email?.Trim();


        model.Phone =
            model.Phone?.Trim();


        model.Role =
            model.Role.Trim();


        model.Status =
            model.Status.Trim();


        if (!string.IsNullOrWhiteSpace(
                model.Password))
        {
            model.Password =
                model.Password.Trim();
        }
    }
}