using System.ComponentModel.DataAnnotations;

namespace SupplierSelectionDSS.Models;

public class UserFormViewModel
{
    public int UserId { get; set; }


    [Required(
        ErrorMessage = "Please enter a username.")]
    [StringLength(
        50,
        ErrorMessage =
            "Username cannot exceed 50 characters.")]
    [Display(
        Name = "Username")]
    public string Username { get; set; } =
        string.Empty;


    [Required(
        ErrorMessage =
            "Please enter a full name.")]
    [StringLength(
        100,
        ErrorMessage =
            "Full name cannot exceed 100 characters.")]
    [Display(
        Name = "Full name")]
    public string FullName { get; set; } =
        string.Empty;


    [EmailAddress(
        ErrorMessage =
            "Invalid email format.")]
    [StringLength(100)]
    [Display(
        Name = "Email")]
    public string? Email { get; set; }


    [StringLength(20)]
    [Display(
        Name = "Phone")]
    public string? Phone { get; set; }


    [Required(
        ErrorMessage =
            "Please select a role.")]
    [Display(
        Name = "Role")]
    public string Role { get; set; } =
        "PurchasingStaff";


    [Required(
        ErrorMessage =
            "Please select a status.")]
    [Display(
        Name = "Status")]
    public string Status { get; set; } =
        "Active";


    [DataType(DataType.Password)]
    [MinLength(
        8,
        ErrorMessage =
            "Password must be at least 8 characters.")]
    [Display(
        Name = "Password")]
    public string? Password { get; set; }
}
