using System.ComponentModel.DataAnnotations;

namespace SupplierSelectionDSS.Models;

public class SupplierFormViewModel
{
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Please enter a supplier code.")]
    [StringLength(
        20,
        ErrorMessage = "Supplier code cannot exceed 20 characters.")]
    [Display(Name = "Supplier code")]
    public string SupplierCode { get; set; } = string.Empty;


    [Required(ErrorMessage = "Please enter a supplier name.")]
    [StringLength(
        200,
        ErrorMessage = "Supplier name cannot exceed 200 characters.")]
    [Display(Name = "Supplier name")]
    public string SupplierName { get; set; } = string.Empty;


    [StringLength(255)]
    [Display(Name = "Address")]
    public string? Address { get; set; }


    [StringLength(100)]
    [Display(Name = "Contact person")]
    public string? ContactPerson { get; set; }


    [EmailAddress(
        ErrorMessage = "Invalid email format.")]
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
