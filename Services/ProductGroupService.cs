using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Constants;
using SupplierSelectionDSS.Models;

namespace SupplierSelectionDSS.Services;

public class ProductGroupService
{
    private readonly ApplicationDbContext _context;

    public ProductGroupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProductGroup>> SearchAsync(
        string? keyword,
        string? status,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var query = _context.ProductGroups
            .AsNoTracking()
            .Include(x => x.Suppliers)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();

            query = query.Where(x =>
                x.GroupCode.Contains(keyword) ||
                x.GroupName.Contains(keyword) ||
                (x.Description != null && x.Description.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            status != "All")
        {
            query = query.Where(x => x.Status == status);
        }

        query = query.OrderBy(x => x.GroupCode);

        return await PagedResult<ProductGroup>.CreateAsync(query, pageNumber, pageSize);
    }

    public async Task<(int Total, int Active, int Inactive, int TotalLinkedSuppliers)> GetKpiSummaryAsync()
    {
        var total = await _context.ProductGroups.CountAsync();
        var active = await _context.ProductGroups.CountAsync(x => x.Status == EntityStatus.Active);
        var inactive = await _context.ProductGroups.CountAsync(x => x.Status == EntityStatus.Inactive);
        var totalLinkedSuppliers = await _context.Suppliers.CountAsync(x => x.Groups.Any());

        return (total, active, inactive, totalLinkedSuppliers);
    }

    public async Task<ProductGroup?> GetByIdAsync(int id)
    {
        return await _context.ProductGroups
            .Include(x => x.Suppliers)
            .AsTracking()
            .FirstOrDefaultAsync(x => x.GroupId == id);
    }

    public async Task<bool> GroupCodeExistsAsync(
        string groupCode,
        int? excludeGroupId = null)
    {
        groupCode = groupCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(groupCode))
        {
            return false;
        }

        var query = _context.ProductGroups
            .AsNoTracking()
            .Where(x => x.GroupCode == groupCode);

        if (excludeGroupId.HasValue)
        {
            query = query.Where(
                x => x.GroupId != excludeGroupId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task CreateAsync(ProductGroup group)
    {
        if (group == null)
        {
            throw new ArgumentNullException(nameof(group));
        }

        group.GroupCode = group.GroupCode?.Trim() ?? string.Empty;
        group.GroupName = group.GroupName?.Trim() ?? string.Empty;
        group.Description = string.IsNullOrWhiteSpace(group.Description)
            ? null
            : group.Description.Trim();

        if (string.IsNullOrWhiteSpace(group.GroupCode))
        {
            throw new InvalidOperationException("Mã nhóm hàng không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(group.GroupName))
        {
            throw new InvalidOperationException("Tên nhóm hàng không được để trống.");
        }

        if (await GroupCodeExistsAsync(group.GroupCode))
        {
            throw new InvalidOperationException("Mã nhóm hàng đã tồn tại.");
        }

        group.Status = EntityStatus.Active;
        group.CreatedAt = DateTime.UtcNow;
        group.UpdatedAt = null;

        _context.ProductGroups.Add(group);

        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductGroup group)
    {
        if (group == null)
        {
            throw new ArgumentNullException(nameof(group));
        }

        var existing = await _context.ProductGroups
            .FirstOrDefaultAsync(x => x.GroupId == group.GroupId);

        if (existing == null)
        {
            throw new KeyNotFoundException("Không tìm thấy nhóm hàng.");
        }

        var groupCode = group.GroupCode?.Trim() ?? string.Empty;
        var groupName = group.GroupName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(groupCode))
        {
            throw new InvalidOperationException("Mã nhóm hàng không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(groupName))
        {
            throw new InvalidOperationException("Tên nhóm hàng không được để trống.");
        }

        if (await GroupCodeExistsAsync(groupCode, group.GroupId))
        {
            throw new InvalidOperationException("Mã nhóm hàng đã tồn tại.");
        }

        existing.GroupCode = groupCode;
        existing.GroupName = groupName;
        existing.Description = string.IsNullOrWhiteSpace(group.Description)
            ? null
            : group.Description.Trim();
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var group = await _context.ProductGroups
            .FirstOrDefaultAsync(x => x.GroupId == id);

        if (group == null)
        {
            return false;
        }

        group.Status =
            group.Status == EntityStatus.Active
                ? EntityStatus.Inactive
                : EntityStatus.Active;

        group.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<Supplier>> GetAllActiveSuppliersAsync()
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(x => x.Status == EntityStatus.Active)
            .OrderBy(x => x.SupplierCode)
            .ToListAsync();
    }

    public async Task<bool> UpdateSuppliersAsync(
        int groupId,
        List<int> supplierIds)
    {
        var group = await _context.ProductGroups
            .Include(x => x.Suppliers)
            .FirstOrDefaultAsync(x => x.GroupId == groupId);

        if (group == null)
        {
            return false;
        }

        if (group.Status != EntityStatus.Active)
        {
            throw new InvalidOperationException(
                "Không thể cập nhật nhà cung cấp cho nhóm hàng đang Inactive.");
        }

        var requestedIds = (supplierIds ?? new List<int>())
            .Distinct()
            .ToList();

        var currentAssignedIds = group.Suppliers
            .Select(x => x.SupplierId)
            .ToHashSet();

        var suppliers = await _context.Suppliers
            .Where(x =>
                requestedIds.Contains(x.SupplierId) &&
                (x.Status == EntityStatus.Active || currentAssignedIds.Contains(x.SupplierId)))
            .ToListAsync();

        if (suppliers.Count != requestedIds.Count)
        {
            throw new InvalidOperationException(
                "Danh sách nhà cung cấp chứa nhà cung cấp không tồn tại hoặc không còn Active.");
        }

        group.Suppliers.Clear();

        foreach (var supplier in suppliers)
        {
            group.Suppliers.Add(supplier);
        }

        group.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}
