using ReadEase.Models;
using ReadEase.Helpers;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Profile;

namespace ReadEase.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepo;
    private readonly IAuthorApplicationRepository _appRepo;
    private readonly IBookRepository _bookRepo;
    private readonly PasswordHasher _hasher;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        IUserRepository userRepo,
        IAuthorApplicationRepository appRepo,
        IBookRepository bookRepo,
        PasswordHasher hasher,
        IWebHostEnvironment env,
        ILogger<ProfileService> logger)
    {
        _userRepo = userRepo;
        _appRepo = appRepo;
        _bookRepo = bookRepo;
        _hasher = hasher;
        _env = env;
        _logger = logger;
    }

    public async Task<ProfileViewModel?> GetProfileAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null) return null;

        var hasPending = await _appRepo.HasPendingApplicationAsync(userId);

        return new ProfileViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            Provider = user.Provider,
            ProfilePhoto = user.ProfilePhoto,
            CoverPhoto = user.CoverPhoto,
            HasPendingApplication = hasPending,
            IsEmailConfirmed = user.IsEmailConfirmed,
            IsSubscribed = user.IsSubscribed
        };
    }

    public async Task PopulateStatsAsync(Guid userId, ProfileViewModel profile)
    {
        var books = await _bookRepo.GetByAuthorUserIdAsync(userId);
        profile.TotalBooks = books.Count;
        // Extend here if you have read/favorite repos injected
        profile.TotalReads = 0;
        profile.TotalFavorites = 0;
    }

    public async Task UpdateProfileAsync(Guid userId, EditProfileViewModel vm)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        user.Name = vm.Name;

        if (vm.ProfilePhoto is { Length: > 0 })
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(vm.ProfilePhoto.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new InvalidOperationException("Only .jpg, .jpeg, .png, and .webp files are allowed.");
            if (vm.ProfilePhoto.Length > 2 * 1024 * 1024)
                throw new InvalidOperationException("Photo must be under 2MB.");

            if (!string.IsNullOrEmpty(user.ProfilePhoto))
            {
                var oldPath = Path.Combine(_env.WebRootPath, user.ProfilePhoto.TrimStart('/'));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            var uploads = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploads);
            var fileName = $"{userId}{ext}";
            var filePath = Path.Combine(uploads, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await vm.ProfilePhoto.CopyToAsync(stream);
            user.ProfilePhoto = $"/uploads/profiles/{fileName}";
        }

        if (vm.CoverPhoto is { Length: > 0 })
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(vm.CoverPhoto.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new InvalidOperationException("Only .jpg, .jpeg, .png, and .webp files are allowed.");
            if (vm.CoverPhoto.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Cover photo must be under 5MB.");

            if (!string.IsNullOrEmpty(user.CoverPhoto))
            {
                var oldPath = Path.Combine(_env.WebRootPath, user.CoverPhoto.TrimStart('/'));
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }

            var uploads = Path.Combine(_env.WebRootPath, "uploads", "profiles", "covers");
            Directory.CreateDirectory(uploads);
            var fileName = $"cover_{userId}{ext}";
            var filePath = Path.Combine(uploads, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await vm.CoverPhoto.CopyToAsync(stream);
            user.CoverPhoto = $"/uploads/profiles/covers/{fileName}";
        }

        await _bookRepo.UpdateAuthorNameAsync(userId, vm.Name);
        _logger.LogInformation("Author name synced on all books for {UserId}", userId);

        await _userRepo.UpdateAsync(user);
        _logger.LogInformation("Profile updated: {UserId}", userId);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordViewModel vm)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.Provider == "Google")
            throw new InvalidOperationException("Google accounts cannot change password here.");

        if (user.PasswordHash is null ||
            !_hasher.VerifyPassword(user, vm.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        user.PasswordHash = _hasher.HashPassword(user, vm.NewPassword);
        await _userRepo.UpdateAsync(user);
        _logger.LogInformation("Password changed: {UserId}", userId);
    }

    public async Task ApplyAsAuthorAsync(Guid userId, ApplyAsAuthorViewModel vm)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.Role.ToString() != "User")
            throw new InvalidOperationException("Only users with the User role can apply.");

        var alreadyPending = await _appRepo.HasPendingApplicationAsync(userId);
        if (alreadyPending)
            throw new InvalidOperationException("You already have a pending application.");

        var application = new Models.AuthorApplication
        {
            UserId = userId,
            Bio = vm.Bio,
            AppliedAt = DateTime.UtcNow
        };

        await _appRepo.CreateAsync(application);
        _logger.LogInformation("Author application submitted: {UserId}", userId);
    }
}