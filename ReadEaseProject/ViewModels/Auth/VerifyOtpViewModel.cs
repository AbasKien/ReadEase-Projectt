using System.ComponentModel.DataAnnotations;

namespace ReadEase.ViewModels.Auth;

public class VerifyOtpViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Please enter the full 6-digit code.")]
    public string Otp { get; set; } = string.Empty;
}