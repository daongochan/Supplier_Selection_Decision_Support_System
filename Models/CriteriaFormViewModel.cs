using System.ComponentModel.DataAnnotations;

namespace SupplierSelectionDSS.Models;

public class CriteriaFormViewModel
{
    public int CriteriaId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã tiêu chí.")]
    [StringLength(
        20,
        ErrorMessage = "Mã tiêu chí tối đa 20 ký tự.")]
    [Display(Name = "Criteria code")]
    public string CriteriaCode { get; set; } = string.Empty;


    [Required(ErrorMessage = "Vui lòng nhập tên tiêu chí.")]
    [StringLength(
        150,
        ErrorMessage = "Tên tiêu chí tối đa 150 ký tự.")]
    [Display(Name = "Criteria name")]
    public string CriteriaName { get; set; } = string.Empty;


    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }


    [Required(ErrorMessage = "Vui lòng chọn loại tiêu chí.")]
    [Display(Name = "Criteria type")]
    public string CriteriaType { get; set; } = "Benefit";


    [StringLength(30)]
    [Display(Name = "Unit")]
    public string? Unit { get; set; }
}