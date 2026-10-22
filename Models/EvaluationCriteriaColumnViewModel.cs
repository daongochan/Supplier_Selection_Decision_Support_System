namespace SupplierSelectionDSS.Models;

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
        SupplierSelectionDSS.Constants.CriteriaUnits.IsRatingScale(Unit);

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
