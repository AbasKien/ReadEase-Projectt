using ReadEase.Models;
using ReadEase.ViewModels.Auth;
using ReadEase.DTOs.Auth;

namespace ReadEase.Services.Interfaces;

public interface IAuthService
{
    // ── MVC (ViewModels) ──────────────────────────────────────────────────
    Task<User> RegisterMvcAsync(RegisterViewModel vm, string confirmationLink);
    Task<bool> ConfirmEmailAsync(string email, string token);
    Task<User?> ValidateUserForMvcAsync(string email, string password);
    Task<User> FindOrCreateExternalUserAsync(
        string provider, string externalId, string email, string name);

    // ── Password Reset ────────────────────────────────────────────────────
    Task<string?> GeneratePasswordResetTokenAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);

    // ── API (DTOs) ────────────────────────────────────────────────────────
    Task<AuthResponseDto> RegisterApiAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginApiAsync(LoginDto dto);
    Task<AuthResponseDto> GoogleSignInAsync(GoogleLoginDto dto);

    // ── Mobile email verification (reuses existing token fields) ──────────
    Task<bool> VerifyEmailAsync(string email, string token);
    Task<(string userName, string token)> GenerateNewVerificationTokenAsync(string email);
    Task<(string userName, string token)> GenerateApiPasswordResetTokenAsync(string email);

    // ── Mobile OTP Password Reset ───────────────────────────────────────────
    Task<(string userName, string otp)> GeneratePasswordResetOtpAsync(string email);
    Task<bool> VerifyPasswordResetOtpAsync(string email, string otp);
    Task<bool> ResetPasswordWithOtpAsync(string email, string otp, string newPassword);
}