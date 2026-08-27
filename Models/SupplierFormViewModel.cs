using System.ComponentModel.DataAnnotations;

namespace SupplierSelectionDSS.Models;

public class SupplierFormViewModel
{
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã nhà cung cấp.")]
    [StringLength(
        20,
        ErrorMessage = "Mã nhà cung cấp tối đa 20 ký tự.")]
    [Display(Name = "Supplier code")]
    public string SupplierCode { get; set; } = string.Empty;


    [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp.")]
    [StringLength(
        200,
        ErrorMessage = "Tên nhà cung cấp tối đa 200 ký tự.")]
    [Display(Name = "Supplier name")]
    public string SupplierName { get; set; } = string.Empty;


    [StringLength(255)]
    [Display(Name = "Address")]
    public string? Address { get; set; }


    [StringLength(100)]
    [Display(Name = "Contact person")]
    public string? ContactPerson { get; set; }


    [EmailAddress(
        ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }


    [StringLength(20)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }


    [StringLength(20)]
    [Display(Name = "Tax code")]
    public string? TaxCode { get; set; }


    [StringLength(255)]
    [Display(Name = "Website")]
    public string? Website { get; set; }


    [StringLength(150)]
    [Display(Name = "Business field")]
    public string? BusinessField { get; set; }


    [StringLength(255)]
    [Display(Name = "Payment terms")]
    public string? PaymentTerms { get; set; }
}