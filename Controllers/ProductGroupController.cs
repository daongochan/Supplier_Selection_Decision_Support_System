using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;
using SupplierSelectionDSS.Constants;

namespace SupplierSelectionDSS.Controllers;

[Authorize(Roles = UserRole.Administrator + "," + UserRole.PurchasingStaff)]
public class ProductGroupController : Controller
{
    private readonly ProductGroupService _groupService;

    public ProductGroupController(ProductGroupService groupService)
    {
        _groupService = groupService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword,
        string status = "All",
        int page = 1,
        int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Max(pageSize, 1);

        var paged = await _groupService.SearchAsync(keyword, status, page, pageSize);
        var kpis = await _groupService.GetKpiSummaryAsync();

        var model = new ProductGroupIndexViewModel
        {
            Keyword = keyword,
            Status = status,
            ProductGroups = paged.Items.ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
            TotalGroups = kpis.Total,
            ActiveGroups = kpis.Active,
            InactiveGroups = kpis.Inactive,
            TotalLinkedSuppliers = kpis.TotalLinkedSuppliers
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProductGroupFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductGroupFormViewModel model)
    {
        Normalize(model);

        if (await _groupService.GroupCodeExistsAsync(model.GroupCode))
        {
            ModelState.AddModelError(
                nameof(model.GroupCode),
                "Mã nhóm hàng đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var group = new ProductGroup
        {
            GroupCode = model.GroupCode,
            GroupName = model.GroupName,
            Description = model.Description
        };

        try
        {
            await _groupService.CreateAsync(group);

            TempData["SuccessMessage"] =
                "Nhóm hàng đã được tạo thành công.";

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var group = await _groupService.GetByIdAsync(id);

        if (group == null)
        {
            return NotFound();
        }

        var model = new ProductGroupFormViewModel
        {
            GroupId = group.GroupId,
            GroupCode = group.GroupCode,
            GroupName = group.GroupName,
            Description = group.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductGroupFormViewModel model)
    {
        Normalize(model);

        if (await _groupService.GroupCodeExistsAsync(
                model.GroupCode,
                model.GroupId))
        {
            ModelState.AddModelError(
                nameof(model.GroupCode),
                "Mã nhóm hàng đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var group = await _groupService.GetByIdAsync(model.GroupId);

        if (group == null)
        {
            return NotFound();
        }

        group.GroupCode = model.GroupCode;
        group.GroupName = model.GroupName;
        group.Description = model.Description;

        try
        {
            await _groupService.UpdateAsync(group);

            TempData["SuccessMessage"] =
                "Nhóm hàng đã được cập nhật.";

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            var result = await _groupService.ToggleStatusAsync(id);

            if (!result)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] =
                "Trạng thái nhóm hàng đã được cập nhật.";

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Suppliers(int id)
    {
        var group = await _groupService.GetByIdAsync(id);

        if (group == null)
        {
            return NotFound();
        }

        var allSuppliers = await _groupService.GetAllActiveSuppliersAsync();

        var selectedSupplierIds = group.Suppliers
            .Select(x => x.SupplierId)
            .ToHashSet();

        var model = new ProductGroupSuppliersViewModel
        {
            GroupId = group.GroupId,
            GroupCode = group.GroupCode,
            GroupName = group.GroupName,
            Suppliers = allSuppliers
                .Select(x => new SupplierSelectionItemViewModel
                {
                    SupplierId = x.SupplierId,
                    SupplierCode = x.SupplierCode,
                    SupplierName = x.SupplierName,
                    Selected = selectedSupplierIds.Contains(x.SupplierId)
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suppliers(
        int id,
        List<int>? supplierIds)
    {
        try
        {
            var result = await _groupService.UpdateSuppliersAsync(
                id,
                supplierIds ?? new List<int>());

            if (!result)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] =
                "Danh sách nhà cung cấp của nhóm đã được cập nhật.";

            return RedirectToAction(nameof(Suppliers), new { id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Suppliers), new { id });
        }
    }

    private static void Normalize(ProductGroupFormViewModel model)
    {
        model.GroupCode = model.GroupCode?.Trim() ?? string.Empty;
        model.GroupName = model.GroupName?.Trim() ?? string.Empty;
        model.Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim();
    }
}
