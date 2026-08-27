using SupplierSelectionDSS.Data.Entities;

namespace SupplierSelectionDSS.Models;

public class EvaluationViewModel
{
    public int BatchId { get; set; }

    public string BatchCode { get; set; } = string.Empty;

    public string BatchName { get; set; } = string.Empty;

    public string GroupCode { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public List<EvaluationCriteriaColumnViewModel> Criteria { get; set; }
        = new();

    public List<EvaluationSupplierRowViewModel> Suppliers { get; set; }
        = new();
}


public class EvaluationCriteriaColumnViewModel
{
    public int CriteriaId { get; set; }

    public string CriteriaCode { get; set; } = string.Empty;

    public string CriteriaName { get; set; } = string.Empty;

    public string CriteriaType { get; set; } = string.Empty;

    public string? Unit { get; set; }


    // =========================================================
    // INPUT TYPE
    // Không lưu vào database.
    // Được suy ra từ Unit của Criteria.
    // =========================================================

    public bool IsRating =>
        string.Equals(
            Unit?.Trim(),
            "Điểm",
            StringComparison.OrdinalIgnoreCase);


    public string InputType =>
        IsRating
            ? "Rating"
            : "Numeric";


    public decimal? MinValue =>
        IsRating
            ? 0m
            : null;


    public decimal? MaxValue =>
        IsRating
            ? 10m
            : null;


    public string InputHint =>
        IsRating
            ? "0–10"
            : "Numeric";
}


public class EvaluationSupplierRowViewModel
{
    public int SupplierId { get; set; }

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public Dictionary<int, EvaluationCellViewModel> Scores { get; set; }
        = new();
}


public class EvaluationCellViewModel
{
    public decimal? Score { get; set; }

    public int? EvaluationScoreId { get; set; }
}