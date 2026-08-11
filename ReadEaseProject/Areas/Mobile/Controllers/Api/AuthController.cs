using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.DTOs.Auth;
using ReadEase.Services;
using ReadEase.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ReadEase.Areas.Mobile.Controllers.Api;

[Area("Mobile")]
[Route("Areas/Mobile/Api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _config;

    public AuthController(
        IAuthService authService,
        IEmailService emailService,
        ILogger<AuthController> logger,
        IConfiguration config)
    {
        _authService = authService;
        _emailService = emailService;
        _logger = logger;
        _config = config;
    }

    // ── POST /register ────────────────────────────────────────────────────
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var result = await _authService.RegisterApiAsync(dto);

            // Send OTP email — awaited directly so we can report failure to the client
            try
            {
                await _emailService.SendConfirmationEmailAsync(dto.Email, result.Name, result.VerificationToken!);
                _logger.LogInformation("✅ OTP email sent to {Email}", dto.Email);
            }
            catch (Exception emailEx)
            {
                // Registration succeeded but email failed — tell the user clearly
                _logger.LogError(emailEx, "❌ OTP email FAILED for {Email}: {Msg}", dto.Email, emailEx.Message);
                return Ok(new
                {
                    requiresVerification = true,
                    emailSent = false,
                    message = $"Account created but we could not send the OTP email: {emailEx.Message}. " +
                               "Use 'Resend OTP' or check your spam folder."
                });
            }

            return Ok(new
            {
                requiresVerification = true,
                emailSent = true,
                message = "Account created! A 6-digit OTP has been sent to your Gmail."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── POST /verify-otp ──────────────────────────────────────────────────
    // User types the 6-digit code from Gmail; returns JWT on success.
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        try
        {
            var success = await _authService.VerifyEmailAsync(dto.Email, dto.Otp);
            if (!success)
                return BadRequest(new { message = "Invalid or expired OTP. Please request a new one." });

            // Auto-login after successful verification — return a JWT
            var loginResult = await _authService.LoginApiAsync(
                new LoginDto { Email = dto.Email, Password = dto.Password });

            return Ok(new
            {
                verified = true,
                token = loginResult.Token,
                name = loginResult.Name,
                email = loginResult.Email,
                role = loginResult.Role,
                message = "Email verified! Welcome to ReadEase."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTP verification failed for {Email}", dto.Email);
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── POST /resend-otp ──────────────────────────────────────────────────
    [HttpPost("resend-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendOtp([FromBody] ResendVerificationDto dto)
    {
        try
        {
            var (userName, otp) = await _authService.GenerateNewVerificationTokenAsync(dto.Email);

            try
            {
                await _emailService.SendConfirmationEmailAsync(dto.Email, userName, otp);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "❌ Resend OTP email FAILED for {Email}: {Msg}", dto.Email, emailEx.Message);
                return StatusCode(500, new { message = $"Could not send OTP email: {emailEx.Message}" });
            }

            return Ok(new { message = "A new OTP has been sent to your Gmail." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── POST /login ───────────────────────────────────────────────────────
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginApiAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── GET /google-client-id ───────────────────────────────────────────
    [HttpGet("google-client-id")]
    [AllowAnonymous]
    public IActionResult GetGoogleClientId()
    {
        var clientId = _config["Google:ClientId"];
        if (string.IsNullOrEmpty(clientId))
        {
            return NotFound(new { message = "Google Sign-In not configured" });
        }
        return Ok(new { clientId });
    }

    // ── POST /google ──────────────────────────────────────────────────────
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleSignIn([FromBody] GoogleLoginDto dto)
    {
        try
        {
            var result = await _authService.GoogleSignInAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── POST /forgot-password ─────────────────────────────────────────────
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            var (userName, token) = await _authService.GenerateApiPasswordResetTokenAsync(dto.Email);
            _ = SendEmailSafe(
                () => _emailService.SendPasswordResetEmailAsync(dto.Email, userName, token),
                dto.Email, "password reset");
        }
        catch (InvalidOperationException ex)
        {
            // Show user-facing errors like "This account uses Google Sign-In and does not have a password."
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ForgotPassword suppressed for {Email}", dto.Email);
        }

        return Ok(new { message = "If that email is registered, a reset link has been sent to your Gmail." });
    }

    // ── POST /forgot-password-otp ──────────────────────────────────────────
    [HttpPost("forgot-password-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordOtp([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            var (userName, otp) = await _authService.GeneratePasswordResetOtpAsync(dto.Email);

            // Await email and report failure to client
            try
            {
                await _emailService.SendPasswordResetOtpEmailAsync(dto.Email, userName, otp);
                _logger.LogInformation("✅ Password reset OTP sent to {Email}", dto.Email);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "❌ Password reset OTP email FAILED for {Email}: {Msg}", dto.Email, emailEx.Message);
                return StatusCode(500, new { message = $"Could not send reset code: {emailEx.Message}" });
            }

            return Ok(new { message = "A 6-digit reset code has been sent to your Gmail." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ForgotPasswordOtp failed for {Email}", dto.Email);
            return StatusCode(500, new { message = "An error occurred. Please try again." });
        }
    }

    // ── POST /reset-password-otp ───────────────────────────────────────────
    [HttpPost("reset-password-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpDto dto)
    {
        try
        {
            var success = await _authService.ResetPasswordWithOtpAsync(dto.Email, dto.Otp, dto.NewPassword);
            if (!success)
                return BadRequest(new { message = "Invalid or expired OTP." });

            return Ok(new { message = "Password reset successfully. You can now log in." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── POST /reset-password ──────────────────────────────────────────────
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            var success = await _authService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
            if (!success)
                return BadRequest(new { message = "Invalid or expired reset link." });

            return Ok(new { message = "Password reset successfully. You can now log in." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── GET /me ───────────────────────────────────────────────────────────
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return Ok(new { userId, email, name, role });
    }

    // ── Private ───────────────────────────────────────────────────────────
    private async Task SendEmailSafe(Func<Task> send, string email, string type)
    {
        try { await send(); }
        catch (Exception ex) { _logger.LogError(ex, "❌ {Type} email failed → {Email}", type, email); }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────
public record ResendVerificationDto(string Email);
public record ForgotPasswordDto(string Email);
public record ResetPasswordDto(string Email, string Token, string NewPassword);
public record ResetPasswordWithOtpDto(string Email, string Otp, string NewPassword);

public class VerifyOtpDto
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;  // needed to auto-login after verify
}