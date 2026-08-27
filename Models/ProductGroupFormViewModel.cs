using System.ComponentModel.DataAnnotations;

namespace SupplierSelectionDSS.Models;

public class ProductGroupFormViewModel
{
    public int GroupId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã nhóm.")]
    [StringLength(20)]
    [Display(Name = "Group code")]
    public string GroupCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên nhóm.")]
    [StringLength(150)]
    [Display(Name = "Group name")]
    public string GroupName { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }
}