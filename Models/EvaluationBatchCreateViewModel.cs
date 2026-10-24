using System.ComponentModel.DataAnnotations;

namespace SupplierSelectionDSS.Models;

public class EvaluationBatchCreateViewModel
{
    [Required(ErrorMessage = "Please enter a batch code.")]
    [StringLength(20)]
    [Display(Name = "Batch code")]
    public string BatchCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a batch name.")]
    [StringLength(200)]
    [Display(Name = "Batch name")]
    public string BatchName { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please select a product group.")]
    [Display(Name = "Product group")]
    public int? GroupId { get; set; }

    [Display(Name = "Start date")]
    public DateOnly? StartDate { get; set; }

    [Display(Name = "End date")]
    public DateOnly? EndDate { get; set; }

    public List<int> SelectedSupplierIds { get; set; } = new();

    public List<int> SelectedCriteriaIds { get; set; } = new();
}
