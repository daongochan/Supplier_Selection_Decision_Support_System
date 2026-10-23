using System.ComponentModel.DataAnnotations;

namespace SupplierSelectionDSS.Models;

public class CriteriaFormViewModel
{
    public int CriteriaId { get; set; }

    [Required(ErrorMessage = "Please enter a criteria code.")]
    [StringLength(
        20,
        ErrorMessage = "Criteria code cannot exceed 20 characters.")]
    [Display(Name = "Criteria code")]
    public string CriteriaCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a criteria name.")]
    [StringLength(
        150,
        ErrorMessage = "Criteria name cannot exceed 150 characters.")]
    [Display(Name = "Criteria name")]
    public string CriteriaName { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please select a criteria type.")]
    [Display(Name = "Criteria type")]
    public string CriteriaType { get; set; } = "Benefit";

    [StringLength(30)]
    [Display(Name = "Unit")]
    public string? Unit { get; set; }
}
