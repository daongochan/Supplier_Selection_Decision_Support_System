using Microsoft.EntityFrameworkCore;
using SupplierSelectionDSS.Data;
using SupplierSelectionDSS.Data.Entities;
using SupplierSelectionDSS.Constants;
namespace SupplierSelectionDSS.Services;

public class CriteriaService
{
    private readonly ApplicationDbContext _context;

    public CriteriaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Criterion>> SearchAsync(
        string? keyword,
        string? type,
        string? status)
    {
        var query = _context.Criteria
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();

            query = query.Where(x =>
                x.CriteriaCode.Contains(keyword) ||
                x.CriteriaName.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(type) &&
            !string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
        {
            type = type.Trim();
            query = query.Where(x => x.CriteriaType == type);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
        {
            status = status.Trim();
            query = query.Where(x => x.Status == status);
        }

        return await query
            .OrderBy(x => x.CriteriaCode)
            .ToListAsync();
    }

    public async Task<Criterion?> GetByIdAsync(int id)
    {
        return await _context.Criteria
            .FirstOrDefaultAsync(x => x.CriteriaId == id);
    }

    public async Task<bool> CriteriaCodeExistsAsync(
        string criteriaCode,
        int? excludeCriteriaId = null)
    {
        var normalizedCode = criteriaCode?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return false;
        }

        var query = _context.Criteria
            .AsNoTracking()
            .Where(x => x.CriteriaCode == normalizedCode);

        if (excludeCriteriaId.HasValue)
        {
            query = query.Where(x => x.CriteriaId != excludeCriteriaId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task CreateAsync(Criterion criterion)
    {
        ArgumentNullException.ThrowIfNull(criterion);

        Normalize(criterion);
        Validate(criterion);

        if (await CriteriaCodeExistsAsync(criterion.CriteriaCode))
        {
            throw new InvalidOperationException("Mã tiêu chí đã tồn tại.");
        }

        criterion.Status = EntityStatus.Active;
        criterion.CreatedAt = DateTime.UtcNow;
        criterion.UpdatedAt = null;

        _context.Criteria.Add(criterion);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Criterion criterion)
    {
        ArgumentNullException.ThrowIfNull(criterion);

        Normalize(criterion);
        Validate(criterion);

        var existing = await _context.Criteria
            .FirstOrDefaultAsync(x => x.CriteriaId == criterion.CriteriaId);

        if (existing == null)
        {
            throw new KeyNotFoundException("Không tìm thấy tiêu chí.");
        }

        if (await CriteriaCodeExistsAsync(
                criterion.CriteriaCode,
                criterion.CriteriaId))
        {
            throw new InvalidOperationException("Mã tiêu chí đã tồn tại.");
        }

        if (!string.Equals(
                existing.CriteriaType,
                criterion.CriteriaType,
                StringComparison.Ordinal))
        {
            var isInUse = await IsUsedInEvaluationOrDecisionAsync(
                criterion.CriteriaId);

            if (isInUse)
            {
                throw new InvalidOperationException(
                    "Không thể thay đổi loại tiêu chí vì tiêu chí đã được sử dụng trong dữ liệu đánh giá hoặc tính toán DSS.");
            }
        }

        existing.CriteriaCode = criterion.CriteriaCode;
        existing.CriteriaName = criterion.CriteriaName;
        existing.Description = criterion.Description;
        existing.CriteriaType = criterion.CriteriaType;
        existing.Unit = criterion.Unit;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var criterion = await _context.Criteria
            .FirstOrDefaultAsync(x => x.CriteriaId == id);

        if (criterion == null)
        {
            return false;
        }

        criterion.Status = string.Equals(
                criterion.Status,
                EntityStatus.Active,
                StringComparison.OrdinalIgnoreCase)
            ? EntityStatus.Inactive
            : EntityStatus.Active;

        criterion.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private static void Normalize(Criterion criterion)
    {
        criterion.CriteriaCode = criterion.CriteriaCode?.Trim() ?? string.Empty;
        criterion.CriteriaName = criterion.CriteriaName?.Trim() ?? string.Empty;
        criterion.Description = criterion.Description?.Trim();
        criterion.CriteriaType = criterion.CriteriaType?.Trim() ?? string.Empty;
        criterion.Unit = criterion.Unit?.Trim();
    }

    private static void Validate(Criterion criterion)
    {
        if (string.IsNullOrWhiteSpace(criterion.CriteriaCode))
        {
            throw new ArgumentException("Mã tiêu chí không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(criterion.CriteriaName))
        {
            throw new ArgumentException("Tên tiêu chí không được để trống.");
        }

        if (!string.Equals(criterion.CriteriaType, CriteriaType.Benefit, StringComparison.Ordinal) &&
            !string.Equals(criterion.CriteriaType, CriteriaType.Cost, StringComparison.Ordinal))
        {
            throw new ArgumentException("Loại tiêu chí không hợp lệ. Chỉ chấp nhận Benefit hoặc Cost.");
        }
    }

    private async Task<bool> IsUsedInEvaluationOrDecisionAsync(int criteriaId)
    {
        return await _context.EvaluationScores
                   .AsNoTracking()
                   .AnyAsync(x => x.CriteriaId == criteriaId)
               || await _context.AhppairwiseComparisons
                   .AsNoTracking()
                   .AnyAsync(x => x.CriteriaRowId == criteriaId || x.CriteriaColId == criteriaId)
               || await _context.AhpcriteriaWeights
                   .AsNoTracking()
                   .AnyAsync(x => x.CriteriaId == criteriaId)
               || await _context.TopsisIdealSolutions
                   .AsNoTracking()
                   .AnyAsync(x => x.CriteriaId == criteriaId)
               || await _context.EvaluationBatches
                   .AsNoTracking()
                   .AnyAsync(x => x.Criteria.Any(c => c.CriteriaId == criteriaId));
    }
}