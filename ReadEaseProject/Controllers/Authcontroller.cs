using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.Enums;
using ReadEase.Models;
using ReadEase.Services;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Auth;
using System.Security.Claims;

namespace ReadEase.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _audit;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IEmailService emailService,
                          IAuditService audit, ITokenService tokenService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _emailService = emailService;
        _audit = audit;
        _tokenService = tokenService;
        _logger = logger;
    }

    // ── Register ──────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Register()
        => View("~/Views/Auth/Register.cshtml", new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Auth/Register.cshtml", vm);

        try
        {
            var user = await _authService.RegisterMvcAsync(vm, string.Empty);

            try
            {
                // Send the 6-digit OTP stored in EmailConfirmationToken
                await _emailService.SendConfirmationEmailAsync(
                    user.Email, user.Name, user.EmailConfirmationToken!);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx,
                    "OTP email failed for {Email}: {Message}",
                    user.Email, emailEx.Message);

                TempData["ErrorMessage"] =
                    $"Account created but we couldn't send the OTP email " +
                    $"({emailEx.GetType().Name}: {emailEx.Message}). " +
                    $"Please use the resend option below.";
            }

            await _audit.LogAsync(user.Id, user.Role.ToString(), "Register",
                $"Email:{user.Email}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());

            // Always redirect to OTP page after register
            return RedirectToAction(nameof(VerifyOtp), new { email = user.Email });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("~/Views/Auth/Register.cshtml", vm);
        }
    }

    // ── Verify OTP ────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult VerifyOtp(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(Register));

        return View("~/Views/Auth/VerifyOtp.cshtml",
            new VerifyOtpViewModel { Email = email });
    }

    [HttpPost]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Auth/VerifyOtp.cshtml", vm);

        var success = await _authService.ConfirmEmailAsync(vm.Email, vm.Otp);

        if (!success)
        {
            TempData["ErrorMessage"] =
                "Invalid or expired code. Please request a new one.";
            return View("~/Views/Auth/VerifyOtp.cshtml", vm);
        }

        TempData["SuccessMessage"] = "✅ Email verified! You can now log in.";
        return RedirectToAction(nameof(Login));
    }

    // ── Resend OTP ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ResendOtp(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(Register));

        try
        {
            var (userName, otp) = await _authService.GenerateNewVerificationTokenAsync(email);

            try
            {
                await _emailService.SendConfirmationEmailAsync(email, userName, otp);
                TempData["SuccessMessage"] = "A new 6-digit code has been sent to your email.";
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx,
                    "Resend OTP email failed for {Email}: {Message}",
                    email, emailEx.Message);
                TempData["ErrorMessage"] =
                    $"Could not send email: {emailEx.Message}. Please try again.";
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(VerifyOtp), new { email });
    }

    // ── Confirm Email (legacy link-based — kept for backwards compat) ──────
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string email, string token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));

        var success = await _authService.ConfirmEmailAsync(email, token);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? "✅ Email confirmed! You can now login."
            : "❌ Invalid or expired confirmation link.";

        return RedirectToAction(nameof(Login));
    }

    // ── Login ─────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login()
        => View("~/Views/Auth/Login.cshtml", new LoginViewModel());

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Auth/Login.cshtml", vm);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var device = Request.Headers["User-Agent"].ToString();

        try
        {
            var user = await _authService.ValidateUserForMvcAsync(vm.Email, vm.Password);

            if (user is null)
            {
                await _audit.LogAsync(null, "Unknown", "LoginFailed",
                    $"Email:{vm.Email}", ip, device);
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View("~/Views/Auth/Login.cshtml", vm);
            }

            await SignInWithCookieAsync(
                user.Id, user.Email, user.Name, user.Role.ToString(),
                user.ProfilePhoto ?? "", user.IsSubscribed);

            await _audit.LogAsync(user.Id, user.Role.ToString(), "Login",
                $"Email:{user.Email}", ip, device);

            return RedirectToDashboard(user.Role.ToString());
        }
        catch (InvalidOperationException ex)
        {
            var msg = ex.Message;

            // Unconfirmed email — redirect to OTP page with helpful message
            if (msg.StartsWith("UNCONFIRMED:"))
            {
                var unconfirmedEmail = msg.Substring("UNCONFIRMED:".Length).Trim();

                await _audit.LogAsync(null, "Unknown", "LoginFailedUnconfirmed",
                    $"Email:{unconfirmedEmail}", ip, device);

                TempData["WarningMessage"] =
                    $"Your email is not verified yet. " +
                    $"<a href='{Url.Action(nameof(VerifyOtp), "Auth", new { email = unconfirmedEmail })}' style='font-weight:800;'>Enter your OTP</a> or " +
                    $"<a href='{Url.Action(nameof(ResendOtp), "Auth", new { email = unconfirmedEmail })}' style='font-weight:800;'>resend a new code</a>.";

                return View("~/Views/Auth/Login.cshtml", vm);
            }

            await _audit.LogAsync(null, "Unknown", "LoginFailed",
                $"Email:{vm.Email} Reason:{msg}", ip, device);

            ModelState.AddModelError(string.Empty, msg);
            return View("~/Views/Auth/Login.cshtml", vm);
        }
    }

    // ── Google ────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult GoogleLogin()
        => Challenge(new AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleCallback", "Auth")
        }, "Google");

    // ── ExternalLogin — entry point used by the mobile Login page ─────────
    // The mobile WebView navigates to this URL; OnNavigating in the MAUI app
    // intercepts it and opens Chrome Custom Tabs for the OAuth flow.
    [HttpGet]
    public IActionResult ExternalLogin(string provider = "Google", bool mobile = false)
    {
        if (mobile)
            HttpContext.Session.SetString("GoogleMobileLogin", "true");

        var redirectUri = Url.Action("GoogleCallback", "Auth");
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, provider);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync("External");

        if (!result.Succeeded || result.Principal is null)
        {
            TempData["ErrorMessage"] = "Google sign-in failed. Please try again.";
            return RedirectToAction(nameof(Login));
        }

        var externalId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var email = result.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
        var name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? "";

        if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Google sign-in failed: missing account info.";
            return RedirectToAction(nameof(Login));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var device = Request.Headers["User-Agent"].ToString();

        var isMobile = HttpContext.Session.GetString("GoogleMobileLogin") == "true";
        HttpContext.Session.Remove("GoogleMobileLogin");

        try
        {
            var user = await _authService.FindOrCreateExternalUserAsync(
                "Google", externalId, email, name);

            await HttpContext.SignOutAsync("External");

            await _audit.LogAsync(user.Id, user.Role.ToString(), "LoginGoogle",
                $"Email:{user.Email}", ip, device);

            if (isMobile)
            {
                // Mobile deep-link flow: redirect to readease://auth/callback
                // Android's IntentFilter in MainActivity intercepts this, closes
                // Chrome Custom Tabs, saves the token, and navigates the app to Home.
                var jwtToken = _tokenService.GenerateJwtToken(user);
                var deepLink = $"readease://auth/callback?token={Uri.EscapeDataString(jwtToken)}&email={Uri.EscapeDataString(user.Email)}";
                return Redirect(deepLink);
            }

            await SignInWithCookieAsync(
                user.Id, user.Email, user.Name, user.Role.ToString(),
                user.ProfilePhoto ?? "", user.IsSubscribed);

            return RedirectToDashboard(user.Role.ToString());
        }
        catch (InvalidOperationException ex)
        {
            await _audit.LogAsync(null, "Unknown", "LoginGoogleFailed",
                $"Email:{email} Reason:{ex.Message}", ip, device);
            TempData["ErrorMessage"] = ex.Message;
            if (isMobile) return Redirect("/Mobile/Login");
            return RedirectToAction(nameof(Login));
        }
    }

    // ── Forgot Password ───────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ForgotPassword()
        => View("~/Views/Auth/ForgotPassword.cshtml", new ForgotPasswordViewModel());

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Auth/ForgotPassword.cshtml", vm);

        try
        {
            var token = await _authService.GeneratePasswordResetTokenAsync(vm.Email);

            if (token is not null)
            {
                var resetLink = Url.Action(
                    "ResetPassword", "Auth",
                    new { email = vm.Email, token },
                    Request.Scheme)!;

                try
                {
                    await _emailService.SendPasswordResetEmailAsync(
                        vm.Email, vm.Email, resetLink);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx,
                        "Password reset email failed for {Email}: {Message}",
                        vm.Email, emailEx.Message);
                }
            }

            TempData["SuccessMessage"] =
                "If that email is registered, a reset link has been sent.";
            return RedirectToAction(nameof(Login));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("~/Views/Auth/ForgotPassword.cshtml", vm);
        }
    }

    // ── Reset Password ────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
        => View("~/Views/Auth/ResetPassword.cshtml",
               new ResetPasswordViewModel { Email = email, Token = token });

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Auth/ResetPassword.cshtml", vm);

        var success = await _authService.ResetPasswordAsync(
            vm.Email, vm.Token, vm.NewPassword);

        if (!success)
        {
            ModelState.AddModelError(string.Empty,
                "Invalid or expired reset link. Please request a new one.");
            return View("~/Views/Auth/ResetPassword.cshtml", vm);
        }

        TempData["SuccessMessage"] = "✅ Password reset successful! You can now login.";
        return RedirectToAction(nameof(Login));
    }

    // ── Logout ────────────────────────────────────────────────────────────
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (Guid.TryParse(userId, out var uid))
        {
            await _audit.LogAsync(uid, role, "Logout",
                $"Email:{email}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());
        }

        await HttpContext.SignOutAsync("Cookies");
        Response.Cookies.Delete("MobileAuthToken");
        return RedirectToAction(nameof(Login));
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private async Task SignInWithCookieAsync(
        Guid id, string email, string name, string role,
        string profilePhoto = "", bool isSubscribed = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString()),
            new(ClaimTypes.Email,          email),
            new(ClaimTypes.Name,           name),
            new(ClaimTypes.Role,           role),
            new("ProfilePhoto",            profilePhoto ?? ""),
            new("IsSubscribed",            isSubscribed.ToString().ToLower())
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync("Cookies", principal);

        // Generate JWT token for mobile API access
        var user = new User
        {
            Id = id,
            Email = email,
            Name = name,
            Role = Enum.Parse<UserRole>(role),
            ProfilePhoto = profilePhoto,
            SubscriptionTier = isSubscribed ? SubscriptionTier.Premium : SubscriptionTier.Free
        };
        var jwtToken = _tokenService.GenerateJwtToken(user);
        Response.Cookies.Append("MobileAuthToken", jwtToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(30)
        });
    }

    private IActionResult RedirectToDashboard(string role) => role switch
    {
        "Admin" => RedirectToAction("Index", "Admin"),
        "Author" => RedirectToAction("AuthorDashboard", "Dashboard"),
        _ => RedirectToAction("UserDashboard", "Dashboard")
    };
}