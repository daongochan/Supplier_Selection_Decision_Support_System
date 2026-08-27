namespace SupplierSelectionDSS.Models;

public class UserIndexViewModel
{
    public string? Keyword { get; set; }

    public string Role { get; set; } = "All";

    public string Status { get; set; } = "All";

    public List<UserListItemViewModel> Users { get; set; } = new();
}


public class UserListItemViewModel
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
}