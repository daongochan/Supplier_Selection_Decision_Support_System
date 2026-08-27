using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Models;

namespace SupplierSelectionDSS.Services;

public class SupplierService
{
    private readonly ApplicationDbContext _context;


    private static readonly int[] AllowedPageSizes =
    {
        10,
        20,
        50
    };


    public SupplierService(
        ApplicationDbContext context)
    {
        _context = context;
    }


    // =========================================================
    // GET ALL
    // =========================================================
    /*
     * Giữ lại phương thức này cho các nghiệp vụ cần toàn bộ
     * Supplier, ví dụ dropdown hoặc lookup.
     *
     * Không dùng phương thức này cho màn hình danh sách.
     */

    public async Task<List<Supplier>> GetAllAsync()
    {
        return await _context.Suppliers
            .AsNoTracking()
            .OrderBy(
                x =>
                    x.SupplierCode)
            .ToListAsync();
    }


    // =========================================================
    // SEARCH + PAGINATION
    // =========================================================

    public async Task<PagedResult<Supplier>> SearchAsync(
        string? keyword,
        string? status,
        int pageNumber = 1,
        int pageSize = 10)
    {
        // =====================================================
        // NORMALIZE PAGINATION
        // =====================================================

        pageNumber =
            Math.Max(
                pageNumber,
                1);


        if (!AllowedPageSizes.Contains(
                pageSize))
        {
            pageSize = 10;
        }


        // =====================================================
        // BASE QUERY
        // =====================================================

        var query =
            _context.Suppliers
                .AsNoTracking()
                .AsQueryable();


        // =====================================================
        // SEARCH
        // =====================================================

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword =
                keyword.Trim();


            query =
                query.Where(
                    x =>
                        x.SupplierCode.Contains(
                            keyword)

                        ||

                        x.SupplierName.Contains(
                            keyword)

                        ||

                        (
                            x.ContactPerson != null
                            &&
                            x.ContactPerson.Contains(
                                keyword)
                        ));
        }


        // =====================================================
        // STATUS
        // =====================================================

        if (
            !string.IsNullOrWhiteSpace(status)
            &&
            status != "All")
        {
            query =
                query.Where(
                    x =>
                        x.Status ==
                        status);
        }


        // =====================================================
        // TOTAL COUNT
        // =====================================================

        var totalCount =
            await query.CountAsync();


        // =====================================================
        // TOTAL PAGES
        // =====================================================

        var totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize);


        // =====================================================
        // PAGE OVERFLOW
        // =====================================================
        /*
         * Nếu người dùng đang ở page 10 nhưng sau khi filter
         * chỉ còn 2 page thì đưa họ về page cuối cùng.
         */

        if (
            totalPages > 0
            &&
            pageNumber > totalPages)
        {
            pageNumber =
                totalPages;
        }


        // =====================================================
        // DATA
        // =====================================================

        var items =
            await query
                .OrderBy(
                    x =>
                        x.SupplierCode)
                .ThenBy(
                    x =>
                        x.SupplierId)
                .Skip(
                    (pageNumber - 1) *
                    pageSize)
                .Take(
                    pageSize)
                .ToListAsync();


        // =====================================================
        // RESULT
        // =====================================================

        return new PagedResult<Supplier>
        {
            Items =
                items,

            PageNumber =
                pageNumber,

            PageSize =
                pageSize,

            TotalCount =
                totalCount,

            TotalPages =
                totalPages
        };
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Supplier?> GetByIdAsync(
        int id)
    {
        return await _context.Suppliers
            .FirstOrDefaultAsync(
                x =>
                    x.SupplierId ==
                    id);
    }


    // =========================================================
    // SUPPLIER CODE EXISTS
    // =========================================================

    public async Task<bool> SupplierCodeExistsAsync(
        string supplierCode,
        int? excludeSupplierId = null)
    {
        var normalizedCode =
            supplierCode.Trim();


        if (
            string.IsNullOrWhiteSpace(
                normalizedCode))
        {
            return false;
        }


        var query =
            _context.Suppliers
                .Where(
                    x =>
                        x.SupplierCode ==
                        normalizedCode);


        if (excludeSupplierId.HasValue)
        {
            query =
                query.Where(
                    x =>
                        x.SupplierId !=
                        excludeSupplierId.Value);
        }


        return await query.AnyAsync();
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task CreateAsync(
        Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(
            supplier);


        supplier.SupplierCode =
            supplier.SupplierCode.Trim();


        supplier.SupplierName =
            supplier.SupplierName.Trim();


        supplier.Status =
            EntityStatus.Active;


        supplier.CreatedAt =
            DateTime.UtcNow;


        supplier.UpdatedAt =
            null;


        if (
            string.IsNullOrWhiteSpace(
                supplier.SupplierCode))
        {
            throw new ArgumentException(
                "Mã nhà cung cấp không được để trống.",
                nameof(supplier));
        }


        if (
            string.IsNullOrWhiteSpace(
                supplier.SupplierName))
        {
            throw new ArgumentException(
                "Tên nhà cung cấp không được để trống.",
                nameof(supplier));
        }


        if (
            await SupplierCodeExistsAsync(
                supplier.SupplierCode))
        {
            throw new InvalidOperationException(
                "Mã nhà cung cấp đã tồn tại.");
        }


        _context.Suppliers.Add(
            supplier);


        await _context.SaveChangesAsync();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task UpdateAsync(
        Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(
            supplier);


        supplier.SupplierCode =
            supplier.SupplierCode.Trim();


        supplier.SupplierName =
            supplier.SupplierName.Trim();


        if (
            string.IsNullOrWhiteSpace(
                supplier.SupplierCode))
        {
            throw new ArgumentException(
                "Mã nhà cung cấp không được để trống.",
                nameof(supplier));
        }


        if (
            string.IsNullOrWhiteSpace(
                supplier.SupplierName))
        {
            throw new ArgumentException(
                "Tên nhà cung cấp không được để trống.",
                nameof(supplier));
        }


        if (
            await SupplierCodeExistsAsync(
                supplier.SupplierCode,
                supplier.SupplierId))
        {
            throw new InvalidOperationException(
                "Mã nhà cung cấp đã tồn tại.");
        }


        var existingSupplier =
            await _context.Suppliers
                .FirstOrDefaultAsync(
                    x =>
                        x.SupplierId ==
                        supplier.SupplierId);


        if (existingSupplier == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy nhà cung cấp.");
        }


        existingSupplier.SupplierCode =
            supplier.SupplierCode;


        existingSupplier.SupplierName =
            supplier.SupplierName;


        existingSupplier.Address =
            supplier.Address?.Trim();


        existingSupplier.ContactPerson =
            supplier.ContactPerson?.Trim();


        existingSupplier.Email =
            supplier.Email?.Trim();


        existingSupplier.Phone =
            supplier.Phone?.Trim();


        existingSupplier.TaxCode =
            supplier.TaxCode?.Trim();


        existingSupplier.Website =
            supplier.Website?.Trim();


        existingSupplier.BusinessField =
            supplier.BusinessField?.Trim();


        existingSupplier.PaymentTerms =
            supplier.PaymentTerms?.Trim();


        existingSupplier.UpdatedAt =
            DateTime.UtcNow;


        await _context.SaveChangesAsync();
    }


    // =========================================================
    // TOGGLE STATUS
    // =========================================================

    public async Task<bool> ToggleStatusAsync(
        int id)
    {
        var supplier =
            await _context.Suppliers
                .FirstOrDefaultAsync(
                    x =>
                        x.SupplierId ==
                        id);


        if (supplier == null)
        {
            return false;
        }


        supplier.Status =
            supplier.Status ==
                EntityStatus.Active

                ? EntityStatus.Inactive

                : EntityStatus.Active;


        supplier.UpdatedAt =
            DateTime.UtcNow;


        await _context.SaveChangesAsync();


        return true;
    }
}