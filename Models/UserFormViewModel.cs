using System.ComponentModel.DataAnnotations;

namespace SupplierSelectionDSS.Models;

public class UserFormViewModel
{
    public int UserId { get; set; }


    [Required(
        ErrorMessage = "Vui lòng nhập username.")]
    [StringLength(
        50,
        ErrorMessage =
            "Username tối đa 50 ký tự.")]
    [Display(
        Name = "Username")]
    public string Username { get; set; } =
        string.Empty;


    [Required(
        ErrorMessage =
            "Vui lòng nhập họ và tên.")]
    [StringLength(
        100,
        ErrorMessage =
            "Họ và tên tối đa 100 ký tự.")]
    [Display(
        Name = "Full name")]
    public string FullName { get; set; } =
        string.Empty;


    [EmailAddress(
        ErrorMessage =
            "Email không đúng định dạng.")]
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
            "Vui lòng chọn vai trò.")]
    [Display(
        Name = "Role")]
    public string Role { get; set; } =
        "PurchasingStaff";


    [Required(
        ErrorMessage =
            "Vui lòng chọn trạng thái.")]
    [Display(
        Name = "Status")]
    public string Status { get; set; } =
        "Active";


    [DataType(DataType.Password)]
    [MinLength(
        8,
        ErrorMessage =
            "Mật khẩu phải có ít nhất 8 ký tự.")]
    [Display(
        Name = "Password")]
    public string? Password { get; set; }
}