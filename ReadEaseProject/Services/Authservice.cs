using ReadEase.DTOs.Auth;
using ReadEase.Enums;
using ReadEase.Helpers;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Auth;

namespace ReadEase.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher _hasher;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository repo,
        ITokenService tokenService,
        PasswordHasher hasher,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _repo = repo;
        _tokenService = tokenService;
        _hasher = hasher;
        _config = config;
        _logger = logger;
    }

    // ── MVC Register ──────────────────────────────────────────────────────
    public async Task<User> RegisterMvcAsync(RegisterViewModel vm, string confirmationLink)
    {
        var existingUser = await _repo.GetByEmailAsync(vm.Email);

        if (existingUser is not null)
        {
            bool isExpiredUnconfirmed = !existingUser.IsEmailConfirmed
                && existingUser.Provider == "Local"
                && (existingUser.EmailConfirmationTokenExpiry == null
                    || existingUser.EmailConfirmationTokenExpiry < DateTime.UtcNow);

            if (isExpiredUnconfirmed)
            {
                await _repo.DeleteAsync(existingUser);
                existingUser = null;
            }
            else if (!existingUser.IsEmailConfirmed)
            {
                throw new InvalidOperationException(
                    "This email is pending verification. Please check your inbox or request a new code.");
            }
            else if (existingUser.Provider == "Google")
            {
                throw new InvalidOperationException(
                    "This email is already linked to a Google account. Please sign in with Google.");
            }
            else
            {
                throw new InvalidOperationException(
                    "This email is already registered. Please login or use forgot password.");
            }
        }

        var user = ManualMapper.ToUser(vm);
        user.PasswordHash = _hasher.HashPassword(user, vm.Password);
        user.Provider = "Local";
        user.IsEmailConfirmed = false;

        // ✅ MUST be GenerateOtp() — gives "391847" not a long random string
        var otp = GenerateOtp();
        user.EmailConfirmationToken = otp;
        user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddMinutes(10);

        await _repo.CreateAsync(user);
        _logger.LogInformation("User registered (MVC): {Email}, OTP={Otp}", user.Email, otp);
        return user;
    }


    // ── MVC Email Confirm ─────────────────────────────────────────────────
    public async Task<bool> ConfirmEmailAsync(string email, string token)
    {
        var user = await _repo.GetByEmailAsync(email);
        if (user is null) return false;
        if (user.IsEmailConfirmed) return true;

        if (user.EmailConfirmationToken != token ||
            user.EmailConfirmationTokenExpiry < DateTime.UtcNow)
            return false;

        user.IsEmailConfirmed = true;
        user.EmailConfirmationToken = null;
        user.EmailConfirmationTokenExpiry = null;

        await _repo.UpdateAsync(user);
        _logger.LogInformation("Email confirmed: {Email}", email);
        return true;
    }

    public async Task<User?> ValidateUserForMvcAsync(string email, string password)
    {
        var user = await _repo.GetByEmailAsync(email);
        if (user is null) return null;

        if (user.Provider == "Google")
            throw new InvalidOperationException(
                "This account uses Google Sign-In. Please login with Google.");

        if (!user.IsEmailConfirmed && user.Role != UserRole.Admin)
            throw new InvalidOperationException(
                "Please confirm your email before logging in. Check your inbox.");

        if (!user.IsActive)
            throw new InvalidOperationException(
                "Your account has been deactivated. Please contact support.");

        if (user.PasswordHash is null) return null;

        if (!_hasher.VerifyPassword(user, password, user.PasswordHash))
        {
            _logger.LogWarning("Failed MVC login: {Email}", email);
            return null;
        }

        return user;
    }

    public async Task<User> FindOrCreateExternalUserAsync(
        string provider, string externalId, string email, string name)
    {
        var user = await _repo.GetByExternalIdAsync(provider, externalId);
        if (user is not null)
        {
            if (!user.IsActive)
                throw new InvalidOperationException(
                    "Your account has been deactivated. Please contact support.");
            return user;
        }

        var existingUser = await _repo.GetByEmailAsync(email);
        if (existingUser is not null && existingUser.Provider == "Local")
            throw new InvalidOperationException(
                "This email is already registered with a password account. " +
                "Please login with your email and password.");

        if (existingUser is not null)
        {
            if (!existingUser.IsActive)
                throw new InvalidOperationException(
                    "Your account has been deactivated. Please contact support.");
            existingUser.ExternalId = externalId;
            existingUser.Provider = provider;
            await _repo.UpdateAsync(existingUser);
            return existingUser;
        }

        user = new User
        {
            Name = name,
            Email = email.ToLowerInvariant(),
            Provider = provider,
            ExternalId = externalId,
            Role = UserRole.User,
            IsEmailConfirmed = true
        };

        await _repo.CreateAsync(user);
        _logger.LogInformation("External user created: {Email} via {Provider}", email, provider);
        return user;
    }

    // ── Password Reset (MVC) ──────────────────────────────────────────────
    public async Task<string?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _repo.GetByEmailAsync(email);
        if (user is null) return null;

        if (user.Provider == "Google")
            throw new InvalidOperationException(
                "This account uses Google Sign-In and does not have a password.");

        var token = GenerateSecureToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _repo.UpdateAsync(user);
        return token;
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _repo.GetByEmailAsync(email);
        if (user is null) return false;

        if (user.PasswordResetToken != token ||
            user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return false;

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _repo.UpdateAsync(user);
        _logger.LogInformation("Password reset for: {Email}", email);
        return true;
    }

    // ── API Register — generates a 6-digit OTP ────────────────────────────
    public async Task<AuthResponseDto> RegisterApiAsync(RegisterDto dto)
    {
        var existingUser = await _repo.GetByEmailAsync(dto.Email);

        if (existingUser is not null)
        {
            if (existingUser.Provider == "Google")
                throw new InvalidOperationException(
                    "This email is already linked to a Google account.");

            throw new InvalidOperationException("Email is already registered.");
        }

        var user = ManualMapper.ToUser(dto);
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        user.Provider = "Local";
        user.IsEmailConfirmed = false;

        // 6-digit OTP — expires in 10 minutes
        var otp = GenerateOtp();
        user.EmailConfirmationToken = otp;
        user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddMinutes(10);

        await _repo.CreateAsync(user);
        _logger.LogInformation("User registered (API): {Email}, OTP={Otp}", user.Email, otp);

        var response = BuildResponse(user);
        response.VerificationToken = otp;
        return response;
    }

    public async Task<AuthResponseDto> LoginApiAsync(LoginDto dto)
    {
        var user = await _repo.GetByEmailAsync(dto.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (user.Provider == "Google")
            throw new InvalidOperationException("This account uses Google Sign-In.");

        if (!user.IsEmailConfirmed)
            throw new UnauthorizedAccessException(
                "Please verify your email OTP before logging in.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException(
                "Your account has been deactivated. Please contact support.");

        if (user.PasswordHash is null ||
            !_hasher.VerifyPassword(user, dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed API login: {Email}", dto.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> GoogleSignInAsync(GoogleLoginDto dto)
    {
        var payload = await GoogleTokenValidator.ValidateIdTokenAsync(
            dto.IdToken, _config["Google:ClientId"]!);

        var user = await FindOrCreateExternalUserAsync(
            "Google", payload.Subject, payload.Email, payload.Name);

        return BuildResponse(user);
    }

    // ── Mobile: verify OTP ────────────────────────────────────────────────
    public async Task<bool> VerifyEmailAsync(string email, string otp)
    {
        var user = await _repo.GetByEmailAsync(email);
        if (user is null) return false;
        if (user.IsEmailConfirmed) return true;

        if (user.EmailConfirmationToken != otp.Trim() ||
            user.EmailConfirmationTokenExpiry < DateTime.UtcNow)
            return false;

        user.IsEmailConfirmed = true;
        user.EmailConfirmationToken = null;
        user.EmailConfirmationTokenExpiry = null;

        await _repo.UpdateAsync(user);
        _logger.LogInformation("Email verified via OTP (API): {Email}", email);
        return true;
    }

    // ── Mobile: resend OTP ────────────────────────────────────────────────
    public async Task<(string userName, string token)> GenerateNewVerificationTokenAsync(string email)
    {
        var user = await _repo.GetByEmailAsync(email)
            ?? throw new InvalidOperationException("No account found with that email.");

        if (user.IsEmailConfirmed)
            throw new InvalidOperationException("This email is already verified.");

        var otp = GenerateOtp();
        user.EmailConfirmationToken = otp;
        user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddMinutes(10);

        await _repo.UpdateAsync(user);
        _logger.LogInformation("OTP regenerated (API): {Email}", email);
        return (user.Name, otp);
    }

    // ── Mobile: forgot password ───────────────────────────────────────────
    public async Task<(string userName, string token)> GenerateApiPasswordResetTokenAsync(string email)
    {
        var user = await _repo.GetByEmailAsync(email)
            ?? throw new InvalidOperationException("No account found with that email.");

        if (user.Provider == "Google")
            throw new InvalidOperationException(
                "This account uses Google Sign-In and does not have a password.");

        var token = GenerateSecureToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _repo.UpdateAsync(user);
        return (user.Name, token);
    }

    // ── Mobile: forgot password OTP ───────────────────────────────────────
    public async Task<(string userName, string otp)> GeneratePasswordResetOtpAsync(string email)
    {
        var user = await _repo.GetByEmailAsync(email)
            ?? throw new InvalidOperationException("No account found with that email.");

        if (user.Provider == "Google")
            throw new InvalidOperationException(
                "This account uses Google Sign-In and does not have a password.");

        var otp = GenerateOtp();
        user.PasswordResetToken = otp; // Reuse the same field for OTP
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(10);

        await _repo.UpdateAsync(user);
        _logger.LogInformation("Password reset OTP generated: {Email}, OTP={Otp}", email, otp);
        return (user.Name, otp);
    }

    public async Task<bool> VerifyPasswordResetOtpAsync(string email, string otp)
    {
        var user = await _repo.GetByEmailAsync(email);
        if (user is null) return false;

        if (user.PasswordResetToken != otp.Trim() ||
            user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return false;

        // OTP is valid - don't clear it yet, wait for password reset
        _logger.LogInformation("Password reset OTP verified: {Email}", email);
        return true;
    }

    public async Task<bool> ResetPasswordWithOtpAsync(string email, string otp, string newPassword)
    {
        var user = await _repo.GetByEmailAsync(email);
        if (user is null) return false;

        if (user.PasswordResetToken != otp.Trim() ||
            user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return false;

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _repo.UpdateAsync(user);
        _logger.LogInformation("Password reset with OTP for: {Email}", email);
        return true;
    }

    // ── Private ───────────────────────────────────────────────────────────
    private AuthResponseDto BuildResponse(User user) => new()
    {
        Token = _tokenService.GenerateJwtToken(user),
        Email = user.Email,
        Name = user.Name,
        Role = user.Role.ToString(),
        Expiration = _tokenService.GetExpiration()
    };

    // 6-digit numeric OTP
    private static string GenerateOtp()
        => Random.Shared.Next(100_000, 999_999).ToString();

    private static string GenerateSecureToken()
        => Convert.ToBase64String(Guid.NewGuid().ToByteArray())
               .Replace("+", "-").Replace("/", "_").Replace("=", "")
           + Convert.ToBase64String(Guid.NewGuid().ToByteArray())
               .Replace("+", "-").Replace("/", "_").Replace("=", "");
}