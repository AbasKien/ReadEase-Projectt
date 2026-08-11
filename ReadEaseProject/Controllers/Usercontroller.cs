using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadEase.Data;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Library;
using ReadEase.ViewModels.Profile;
using System.Security.Claims;

namespace ReadEase.Controllers;

[Authorize]
public class UserController : Controller
{
    private readonly IProfileService _profileService;
    private readonly ILibraryService _libraryService;
    private readonly AppDbContext _context;

    public UserController(
        IProfileService profileService,
        ILibraryService libraryService,
        AppDbContext context)
    {
        _profileService = profileService;
        _libraryService = libraryService;
        _context = context;
    }

    // GET /User/Profile
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = GetUserId();
        var profile = await _profileService.GetProfileAsync(userId);
        if (profile is null) return RedirectToAction("Login", "Auth");

        // ── Stats ──────────────────────────────────────────────────────
        profile.TotalFavorites = await _context.Favorites
            .CountAsync(f => f.UserId == userId);

        profile.TotalReads = await _context.BookReads
            .Where(r => r.UserId == userId)
            .Select(r => r.BookId)
            .Distinct()
            .CountAsync();

        if (profile.Role == "Author")
        {
            profile.TotalBooks = await _context.Books
                .CountAsync(b => b.PublishedByUserId == userId && !b.IsDeleted);
        }

        // ── Favorites inline ───────────────────────────────────────────
        var favorites = await _libraryService.GetFavoritesAsync(userId);
        var bookIds = favorites.Select(f => f.Book.Id).ToList();
        var readCounts = await _libraryService.GetReadCountsBatchAsync(bookIds);

        profile.Favorites = favorites.Select(f => new BookCardViewModel
        {
            Id = f.Book.Id,
            Title = f.Book.Title,
            Author = f.Book.Author,
            Category = f.Book.Category,
            CoverImage = f.Book.CoverImage,
            IsFree = f.Book.IsFree,
            IsFavorite = true,
            ReadCount = readCounts.GetValueOrDefault(f.Book.Id, 0)
        }).ToList();

        return View(profile);
    }

    // POST /User/EditProfile
    [HttpPost]
    public async Task<IActionResult> EditProfile(EditProfileViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please fix the errors below.";
            return RedirectToAction(nameof(Profile));
        }

        try
        {
            await _profileService.UpdateProfileAsync(GetUserId(), vm);

            var user = await _profileService.GetProfileAsync(GetUserId());
            if (user is not null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Name,           user.Name),
                    new(ClaimTypes.Email,          user.Email),
                    new(ClaimTypes.Role,           user.Role),
                    new("ProfilePhoto",            user.ProfilePhoto ?? "")
                };
                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync("Cookies", principal);
            }

            TempData["SuccessMessage"] = "Profile updated successfully!";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Profile));
    }

    // POST /User/ChangePassword
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please fix the errors in the password form.";
            TempData["ActiveTab"] = "password";
            return RedirectToAction(nameof(Profile));
        }

        try
        {
            await _profileService.ChangePasswordAsync(GetUserId(), vm);
            TempData["SuccessMessage"] = "Password changed successfully!";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            TempData["ActiveTab"] = "password";
        }

        return RedirectToAction(nameof(Profile));
    }

    // POST /User/ApplyAsAuthor
    [HttpPost]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> ApplyAsAuthor(ApplyAsAuthorViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Bio must be at least 50 characters.";
            return RedirectToAction(nameof(Profile));
        }

        try
        {
            await _profileService.ApplyAsAuthorAsync(GetUserId(), vm);
            TempData["SuccessMessage"] = "Application submitted! An admin will review it shortly.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Profile));
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
}