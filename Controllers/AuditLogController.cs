using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierSelectionDSS.Services;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Controllers;

[Authorize(Roles = UserRole.Administrator)]
public class AuditLogController : Controller
{
    private readonly AuditLogService _auditLogService;


    public AuditLogController(
        AuditLogService auditLogService)
    {
        _auditLogService =
            auditLogService;
    }


    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword,
        string actionType = "All",
        int? userId = null,
        int page = 1,
        int pageSize = 15)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Max(pageSize, 1);

        var model =
            await _auditLogService
                .SearchAsync(
                    keyword,
                    actionType,
                    userId,
                    page,
                    pageSize);


        return View(model);
    }
}