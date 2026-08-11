using ReadEase.ViewModels.Library;
using System.ComponentModel.DataAnnotations;

namespace ReadEase.ViewModels.Profile;

public class ProfileViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ProfilePhoto { get; set; }
    public string? CoverPhoto { get; set; }
    public bool HasPendingApplication { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public bool? IsSubscribed { get; set; }

    // ── Stats ────────────────────────────────────────────────────────────
    public int? TotalBooks { get; set; }
    public int? TotalFavorites { get; set; }
    public int? TotalReads { get; set; }

    // ── Favorites (loaded inline on profile page) ─────────────────────
    public List<BookCardViewModel> Favorites { get; set; } = new();
}

public class EditProfileViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public IFormFile? ProfilePhoto { get; set; }
    public IFormFile? CoverPhoto { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ApplyAsAuthorViewModel
{
    [Required(ErrorMessage = "Bio is required.")]
    [MinLength(50, ErrorMessage = "Bio must be at least 50 characters.")]
    [MaxLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters.")]
    public string Bio { get; set; } = string.Empty;
}