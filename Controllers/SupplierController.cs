using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;
using SupplierSelectionDSS.Services;

namespace SupplierSelectionDSS.Controllers;

[Authorize(
    Roles =
        UserRole.Administrator +
        "," +
        UserRole.PurchasingStaff)]
public class SupplierController : Controller
{
    private readonly SupplierService _supplierService;


    private static readonly int[] AllowedPageSizes =
    {
        10,
        20,
        50
    };


    public SupplierController(
        SupplierService supplierService)
    {
        _supplierService =
            supplierService;
    }


    // =========================================================
    // INDEX
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword,
        string status = "All",
        int? groupId = null,
        int page = 1,
        int pageSize = 10)
    {
        if (!AllowedPageSizes.Contains(
                pageSize))
        {
            pageSize = 10;
        }


        page =
            Math.Max(
                page,
                1);


        var result =
            await _supplierService
                .SearchAsync(
                    keyword,
                    status,
                    groupId,
                    page,
                    pageSize);

        var (total, active, inactive, totalGroups) =
            await _supplierService
                .GetKpiSummaryAsync();

        var activeGroups =
            await _supplierService
                .GetActiveGroupsAsync();


        var model =
            new SupplierIndexViewModel
            {
                Keyword =
                    keyword,

                Status =
                    status,

                GroupId =
                    groupId,

                Groups =
                    activeGroups,

                TotalSuppliers =
                    total,

                ActiveSuppliers =
                    active,

                InactiveSuppliers =
                    inactive,

                TotalGroups =
                    totalGroups,

                Suppliers =
                    result.Items
                        .ToList(),

                PageNumber =
                    result.PageNumber,

                PageSize =
                    result.PageSize,

                TotalCount =
                    result.TotalCount,

                TotalPages =
                    result.TotalPages
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
            new SupplierFormViewModel());
    }


    // =========================================================
    // CREATE - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SupplierFormViewModel model)
    {
        Normalize(
            model);


        if (
            await _supplierService
                .SupplierCodeExistsAsync(
                    model.SupplierCode))
        {
            ModelState.AddModelError(
                nameof(model.SupplierCode),
                "Mã nhà cung cấp đã tồn tại.");
        }


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        var supplier =
            new Supplier
            {
                SupplierCode =
                    model.SupplierCode,

                SupplierName =
                    model.SupplierName,

                Address =
                    model.Address,

                ContactPerson =
                    model.ContactPerson,

                Email =
                    model.Email,

                Phone =
                    model.Phone,

                TaxCode =
                    model.TaxCode,

                Website =
                    model.Website,

                BusinessField =
                    model.BusinessField,

                PaymentTerms =
                    model.PaymentTerms
            };


        try
        {
            await _supplierService
                .CreateAsync(
                    supplier);


            TempData["SuccessMessage"] =
                "Nhà cung cấp đã được tạo thành công.";


            return RedirectToAction(
                nameof(Index));
        }
        catch (
            InvalidOperationException ex)
        {
            ModelState.AddModelError(
                nameof(model.SupplierCode),
                ex.Message);


            return View(model);
        }
        catch (
            ArgumentException ex)
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
        var supplier =
            await _supplierService
                .GetByIdAsync(
                    id);


        if (supplier == null)
        {
            return NotFound();
        }


        var model =
            new SupplierFormViewModel
            {
                SupplierId =
                    supplier.SupplierId,

                SupplierCode =
                    supplier.SupplierCode,

                SupplierName =
                    supplier.SupplierName,

                Address =
                    supplier.Address,

                ContactPerson =
                    supplier.ContactPerson,

                Email =
                    supplier.Email,

                Phone =
                    supplier.Phone,

                TaxCode =
                    supplier.TaxCode,

                Website =
                    supplier.Website,

                BusinessField =
                    supplier.BusinessField,

                PaymentTerms =
                    supplier.PaymentTerms
            };


        return View(model);
    }


    // =========================================================
    // EDIT - POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        SupplierFormViewModel model)
    {
        Normalize(
            model);


        if (
            await _supplierService
                .SupplierCodeExistsAsync(
                    model.SupplierCode,
                    model.SupplierId))
        {
            ModelState.AddModelError(
                nameof(model.SupplierCode),
                "Mã nhà cung cấp đã tồn tại.");
        }


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        var supplier =
            await _supplierService
                .GetByIdAsync(
                    model.SupplierId);


        if (supplier == null)
        {
            return NotFound();
        }


        supplier.SupplierCode =
            model.SupplierCode;


        supplier.SupplierName =
            model.SupplierName;


        supplier.Address =
            model.Address;


        supplier.ContactPerson =
            model.ContactPerson;


        supplier.Email =
            model.Email;


        supplier.Phone =
            model.Phone;


        supplier.TaxCode =
            model.TaxCode;


        supplier.Website =
            model.Website;


        supplier.BusinessField =
            model.BusinessField;


        supplier.PaymentTerms =
            model.PaymentTerms;


        try
        {
            await _supplierService
                .UpdateAsync(
                    supplier);


            TempData["SuccessMessage"] =
                "Thông tin nhà cung cấp đã được cập nhật.";


            return RedirectToAction(
                nameof(Index));
        }
        catch (
            InvalidOperationException ex)
        {
            ModelState.AddModelError(
                nameof(model.SupplierCode),
                ex.Message);


            return View(model);
        }
        catch (
            ArgumentException ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);


            return View(model);
        }
        catch (
            KeyNotFoundException)
        {
            return NotFound();
        }
    }


    // =========================================================
    // TOGGLE STATUS
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(
        int id)
    {
        try
        {
            var result =
                await _supplierService
                    .ToggleStatusAsync(
                        id);


            if (!result)
            {
                return NotFound();
            }


            TempData["SuccessMessage"] =
                "Trạng thái nhà cung cấp đã được cập nhật.";
        }
        catch (
            InvalidOperationException ex)
        {
            TempData["ErrorMessage"] =
                ex.Message;
        }


        return RedirectToAction(
            nameof(Index));
    }


    // =========================================================
    // NORMALIZE FORM
    // =========================================================

    private static void Normalize(
        SupplierFormViewModel model)
    {
        model.SupplierCode =
            model.SupplierCode?.Trim()
            ?? string.Empty;


        model.SupplierName =
            model.SupplierName?.Trim()
            ?? string.Empty;


        model.Address =
            model.Address?.Trim();


        model.ContactPerson =
            model.ContactPerson?.Trim();


        model.Email =
            model.Email?.Trim();


        model.Phone =
            model.Phone?.Trim();


        model.TaxCode =
            model.TaxCode?.Trim();


        model.Website =
            model.Website?.Trim();


        model.BusinessField =
            model.BusinessField?.Trim();


        model.PaymentTerms =
            model.PaymentTerms?.Trim();
    }
}
