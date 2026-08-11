using System.ComponentModel.DataAnnotations;

namespace ReadEase.Areas.Mobile.ViewModels;

public class MobileEditProfileViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? ProfilePhoto { get; set; }
    public bool IsEmailConfirmed { get; set; }

    // Photo upload
    public IFormFile? ProfilePhotoFile { get; set; }

    // Password change (optional)
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmNewPassword { get; set; }
}
