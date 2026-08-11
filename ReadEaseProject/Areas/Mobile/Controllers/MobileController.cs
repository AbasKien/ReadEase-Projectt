using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.Services;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Author;
using ReadEase.ViewModels.Library;
using System.Security.Claims;
using ReadEase.ViewModels.Subscribe;
using ReadEase.Enums;
using ReadEase.Helpers;

namespace ReadEase.Area.Mobile.Controllers;

[Area("Mobile")]
[Route("[area]/[action]")]
[AllowAnonymous]
public class MobileController : Controller
{
    private readonly ILibraryService _library;
    private readonly IAuthService _auth;
    private readonly IAuthorService _author;
    private readonly INotificationService _notifications;
    private readonly IChapterService _chapter;
    private readonly ILogger<MobileController> _logger;
    private readonly ISubscriptionService _subscription;
    private readonly IWebHostEnvironment _env;
    private readonly ITokenService _tokenService;
    private readonly IProfileService _profile;

    public MobileController(
        ILibraryService library,
        IAuthService auth,
        IAuthorService author,
        INotificationService notifications,
        IChapterService chapter,
        ILogger<MobileController> logger,
        ISubscriptionService subscription,
        IWebHostEnvironment env,
        ITokenService tokenService,
        IProfileService profile)
    {
        _library = library;
        _auth = auth;
        _author = author;
        _notifications = notifications;
        _chapter = chapter;
        _logger = logger;
        _subscription = subscription;
        _env = env;
        _tokenService = tokenService;
        _profile = profile;
    }

    // ── Auth helpers ──────────────────────────────────────────────────────

    private async Task<bool> CheckLoginAsync()
    {
        var result = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (result.Succeeded && result.Principal is not null)
        {
            HttpContext.User = result.Principal;
            return true;
        }
        return false;
    }

    private Guid? CurrentUserId
    {
        get
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(v, out var id) ? id : null;
        }
    }

    // ── FIX: Always read tier fresh from DB — never trust the stale JWT claim ──
    // The JWT is baked at login time. When admin approves a subscription the DB
    // is updated but the user's token is NOT re-issued, so any claim-based check
    // will always return the old (Free) tier until the user logs out and back in.
    private async Task<SubscriptionTier> GetCurrentTierAsync()
    {
        // Staff bypass — treat as highest tier so they can always access everything
        if (User.IsInRole("Admin") || User.IsInRole("Author"))
            return SubscriptionTier.PremiumPlus;

        var userId = CurrentUserId ?? Guid.Empty;
        if (userId == Guid.Empty) return SubscriptionTier.Free;

        return await _subscription.GetCurrentTierAsync(userId);
    }

    // Convenience: true for Premium and PremiumPlus (and staff)
    private async Task<bool> GetIsSubscribedAsync()
    {
        var tier = await GetCurrentTierAsync();
        return tier >= SubscriptionTier.Premium;
    }

    private string RedirectToLogin(string returnUrl = "") =>
        Url.Action("Login", "Mobile", new { area = "Mobile", returnUrl })!;

    // ── GET /Mobile/Login ─────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (await CheckLoginAsync()) return RedirectToAction("Home");
        ViewBag.ReturnUrl = returnUrl ?? "/Mobile/Home";
        return View();
    }

    // ── GET /Mobile/Register ──────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (await CheckLoginAsync()) return RedirectToAction("Home");
        return View();
    }

    // ── GET /Mobile/ForgotPassword ───────────────────────────────────────
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // ── GET /Mobile/ResetPassword ───────────────────────────────────────
    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        ViewBag.Email = email;
        ViewBag.Token = token;
        return View();
    }

    // ── GET /Mobile/Logout ────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("MobileAuthToken");
        Response.Cookies.Delete("MobileAuth");
        return RedirectToAction("Login");
    }

    // ── GET /Mobile/Home ──────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Home(
        [FromQuery] string? search,
        [FromQuery] string? category)
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Home"));

        var userId = CurrentUserId ?? Guid.Empty;
        var isSubscribed = await GetIsSubscribedAsync(); // FIX: DB lookup

        var (books, favoriteIds) = await _library.GetLibraryAsync(userId, isSubscribed, search, category);
        var categories = await _library.GetCategoriesAsync();

        // ── Featured sections (only on main home page, not during search/filter) ──
        var topPicks = new List<BookCardViewModel>();
        var newReleases = new List<BookCardViewModel>();
        var recommended = new List<BookCardViewModel>();

        if (string.IsNullOrEmpty(search) && string.IsNullOrEmpty(category))
        {
            var featuredTopPicks = await _library.GetFeaturedTopPicksAsync(10);
            var featuredNewReleases = await _library.GetFeaturedNewReleasesAsync(10);
            var featuredRecommended = await _library.GetFeaturedRecommendedAsync(userId, 10);

            var featuredIds = featuredTopPicks.Select(b => b.Id)
                .Concat(featuredNewReleases.Select(b => b.Id))
                .Concat(featuredRecommended.Select(b => b.Id))
                .Distinct();
            var featuredReadCounts = await _library.GetReadCountsBatchAsync(featuredIds);

            topPicks = featuredTopPicks.Select(b => new BookCardViewModel
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category,
                CoverImage = b.CoverImage,
                IsFree = b.IsFree,
                IsFavorite = favoriteIds.Contains(b.Id),
                ReadCount = featuredReadCounts.GetValueOrDefault(b.Id, 0)
            }).ToList();

            newReleases = featuredNewReleases.Select(b => new BookCardViewModel
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category,
                CoverImage = b.CoverImage,
                IsFree = b.IsFree,
                IsFavorite = favoriteIds.Contains(b.Id),
                ReadCount = featuredReadCounts.GetValueOrDefault(b.Id, 0)
            }).ToList();

            recommended = featuredRecommended.Select(b => new BookCardViewModel
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category,
                CoverImage = b.CoverImage,
                IsFree = b.IsFree,
                IsFavorite = favoriteIds.Contains(b.Id),
                ReadCount = featuredReadCounts.GetValueOrDefault(b.Id, 0)
            }).ToList();
        }

        var vm = new LibraryViewModel
        {
            Books = books.Select(MobileMapper.ToCard).ToList(),
            Categories = categories,
            SearchQuery = search,
            SelectedCategory = category,
            IsSubscribed = isSubscribed,
            IsAdmin = User.IsInRole("Admin"),
            OngoingCount = await _library.GetOngoingCountAsync(userId),
            CompletedCount = await _library.GetCompletedCountAsync(userId),
            TopPicks = topPicks,
            NewReleases = newReleases,
            RecommendedForYou = recommended
        };

        return View(vm);
    }

    // ── GET /Mobile/Detail/{id} ───────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin($"/Mobile/Detail/{id}"));

        var userId = CurrentUserId ?? Guid.Empty;
        var isAdmin = User.IsInRole("Admin");
        var isAuthor = User.IsInRole("Author");

        // FIX: read tier from DB, not from _library.IsPremiumPlusAsync which
        // may also be stale if it reads from a cached claim.
        var tier = await GetCurrentTierAsync();
        var isPremiumPlus = tier == SubscriptionTier.PremiumPlus;
        var isPremium = tier >= SubscriptionTier.Premium || isAdmin || isAuthor;

        var vm = await _library.GetBookDetailAsync(userId, id, isPremium, isAdmin, isPremiumPlus);
        if (vm is null) return NotFound();

        ViewBag.IsPremium = isPremium;
        ViewBag.IsPremiumPlus = isPremiumPlus || isAdmin || isAuthor; // Admin/Author can download too
        ViewBag.BypassGate = isAdmin || isAuthor;

        return View(vm);
    }

    // ── GET /Mobile/Read/{id} ─────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Read(Guid id, [FromQuery] int chapter = 1)
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin($"/Mobile/Read/{id}"));

        var userId = CurrentUserId ?? Guid.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";
        var isSubscribed = await GetIsSubscribedAsync(); // FIX: DB lookup

        var vm = await _library.OpenBookAsync(userId, id, isSubscribed, role);
        if (vm is null) return NotFound();

        if (chapter > 0) vm.CurrentChapter = chapter;

        return View(vm);
    }

    // ── GET /Mobile/Ongoing ───────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Ongoing()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Ongoing"));

        var userId = CurrentUserId ?? Guid.Empty;
        var isSubscribed = await GetIsSubscribedAsync(); // FIX: DB lookup

        var vm = await _library.GetOngoingBooksAsync(userId, isSubscribed);
        ViewBag.ActiveTab = "ongoing";
        return View(vm);
    }

    // ── GET /Mobile/Completed ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Completed()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Completed"));

        var userId = CurrentUserId ?? Guid.Empty;
        var isSubscribed = await GetIsSubscribedAsync(); // FIX: DB lookup

        var vm = await _library.GetCompletedBooksAsync(userId, isSubscribed);
        ViewBag.ActiveTab = "completed";
        return View(vm);
    }

    // ── GET /Mobile/Notifications ─────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Notifications()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Notifications"));

        var userId = CurrentUserId ?? Guid.Empty;
        var notifs = await _notifications.GetForUserAsync(userId);
        await _notifications.MarkAllReadAsync(userId);
        return View(notifs);
    }

    // ── GET /Mobile/Downloads ───────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Downloads()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Downloads"));

        var userId = CurrentUserId ?? Guid.Empty;
        var tier = await GetCurrentTierAsync();
        var isPremiumPlus = tier == SubscriptionTier.PremiumPlus;

        ViewBag.IsPremiumPlus = isPremiumPlus;
        ViewBag.CanDownload = isPremiumPlus || User.IsInRole("Admin") || User.IsInRole("Author");

        return View();
    }

    // ── GET /Mobile/DownloadBook/{id} ────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> DownloadBook(Guid id)
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin($"/Mobile/Detail/{id}"));

        var userId = CurrentUserId ?? Guid.Empty;
        var tier = await GetCurrentTierAsync();
        var isPremiumPlus = tier == SubscriptionTier.PremiumPlus
                         || User.IsInRole("Admin")
                         || User.IsInRole("Author");

        var book = await _library.GetBookByIdAsync(id);
        if (book is null) return NotFound();

        if (!isPremiumPlus)
        {
            TempData["ErrorMessage"] = "Downloads require a Premium++ subscription.";
            return RedirectToAction("Detail", new { id });
        }

        var chapterData = await _chapter.GetReaderDataAsync(id, null,
            isPremiumPlus: true, bypassGate: true);
        var chapters = chapterData?.AllChapters?
            .OrderBy(c => c.ChapterNumber)
            .Select(c => (c.ChapterNumber, c.Title, c.Content ?? string.Empty))
            .ToList();

        var html = BookDownloadHelper.BuildBookHtml(book.Title, book.Author, book.Category, book.Description, chapters, book.Content);
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
        var fileName = BookDownloadHelper.SanitizeFileName(book.Title) + ".html";
        // application/octet-stream forces the browser to download instead of rendering inline
        return File(bytes, "application/octet-stream", fileName);
    }

    // ── GET /Mobile/DownloadChapter ────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> DownloadChapter(Guid chapterId, Guid bookId)
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin($"/Mobile/Detail/{bookId}"));

        var userId = CurrentUserId ?? Guid.Empty;
        var tier = await GetCurrentTierAsync();
        var isPremiumPlus = tier == SubscriptionTier.PremiumPlus
                         || User.IsInRole("Admin")
                         || User.IsInRole("Author");

        var book = await _library.GetBookByIdAsync(bookId);
        if (book is null) return NotFound();

        if (!isPremiumPlus)
        {
            TempData["ErrorMessage"] = "Downloads require a Premium++ subscription.";
            return RedirectToAction("Detail", new { id = bookId });
        }

        var chapterData = await _chapter.GetReaderDataAsync(bookId, null,
            isPremiumPlus: true, bypassGate: true);
        var chapter = chapterData?.AllChapters?.FirstOrDefault(c => c.Id == chapterId);
        if (chapter is null) return NotFound();

        var chapters = new List<(int, string, string)>
        {
            (chapter.ChapterNumber, chapter.Title, chapter.Content ?? string.Empty)
        };

        var html = BookDownloadHelper.BuildBookHtml(book.Title, book.Author, book.Category, null, chapters, null, singleChapter: true);
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
        var fileName = $"{BookDownloadHelper.SanitizeFileName(book.Title)}_Ch{chapter.ChapterNumber}.html";
        // application/octet-stream forces the browser to download instead of rendering inline
        return File(bytes, "application/octet-stream", fileName);
    }

    // ── GET /Mobile/Write ─────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Write()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Write"));

        var userId = CurrentUserId ?? Guid.Empty;
        var vm = await _author.GetMyBooksAsync(userId);
        return View(vm);
    }

    // ── GET /Mobile/PublishBook ───────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> PublishBook()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/PublishBook"));
        return View(new ReadEase.ViewModels.Author.PublishBookViewModel());
    }

    // ── GET /Mobile/EditBook/{bookId} ─────────────────────────────────────
    [HttpGet("{bookId:guid}")]
    public async Task<IActionResult> EditBook(Guid bookId)
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin($"/Mobile/EditBook/{bookId}"));

        var userId = CurrentUserId ?? Guid.Empty;
        var vm = await _author.GetBookForEditAsync(bookId, userId);
        if (vm is null) return NotFound();
        return View(vm);
    }

    // ── GET /Mobile/ManageChapters/{bookId} ───────────────────────────────
    [HttpGet("{bookId:guid}")]
    public async Task<IActionResult> ManageChapters(Guid bookId)
    {
        if (!await CheckLoginAsync())
            return Redirect(RedirectToLogin($"/Mobile/ManageChapters/{bookId}"));

        var userId = CurrentUserId ?? Guid.Empty;

        try
        {
            var dto = await _chapter.GetManageViewAsync(bookId, userId);

            var vm = new ManageChaptersViewModel
            {
                BookId = dto.BookId,
                BookTitle = dto.BookTitle,
                CoverImage = dto.CoverImage,
                IsStoryComplete = dto.IsStoryComplete,
                Chapters = dto.Chapters.Select(c => new ChapterListItem
                {
                    Id = c.Id,
                    ChapterNumber = c.ChapterNumber,
                    Title = c.Title,
                    PublishedAt = c.PublishedAt
                }).ToList()
            };

            return View(vm);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Book not found"))
        {
            return View("ManageChapters", null);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ManageChapters for book {BookId} by user {UserId}", bookId, userId);
            return StatusCode(500, "An error occurred while loading chapters.");
        }
    }

    // ── GET /Mobile/Dashboard ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Dashboard"));

        var userId = CurrentUserId ?? Guid.Empty;
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";

        if (User.IsInRole("Author") || User.IsInRole("Admin"))
        {
            var vm = await _author.GetDashboardAsync(userId, userName, userEmail);
            // FIX: Get fresh profile photo from database (not cached JWT claim)
            var profile = await _profile.GetProfileAsync(userId);
            ViewBag.ProfilePhoto = profile?.ProfilePhoto;
            ViewBag.UserName = profile?.Name ?? userName;
            return View("AuthorDashboard", vm);
        }

        var favorites = await _library.GetFavoritesAsync(userId);
        var isSubscribed = await GetIsSubscribedAsync(); // FIX: DB lookup

        var udVm = new ReadEase.ViewModels.User.UserDashboardViewModel
        {
            Id = userId,
            Name = userName,
            Email = userEmail,
            Provider = User.FindFirstValue("Provider") ?? "",
            ProfilePhoto = User.FindFirstValue("ProfilePhoto"),
            IsSubscribed = isSubscribed,
            HasPendingApplication = false,
            IsEmailConfirmed = User.FindFirstValue("EmailConfirmed") == "true",
            BooksRead = await _library.GetCompletedCountAsync(userId),
            FavoritesCount = favorites.Count
        };
        return View("UserDashboard", udVm);
    }

    // ── GET /Mobile/Profile ───────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Profile"));

        var userId = CurrentUserId ?? Guid.Empty;
        var isSubscribed = await GetIsSubscribedAsync();

        // Fetch fresh profile data from database instead of using cached claims
        var profile = await _profile.GetProfileAsync(userId);
        if (profile is null) return NotFound();

        var vm = new ReadEase.ViewModels.Profile.ProfileViewModel
        {
            Id = userId,
            Name = profile.Name,
            Email = profile.Email,
            Role = User.IsInRole("Admin") ? "Admin"
                             : User.IsInRole("Author") ? "Author" : "Reader",
            Provider = User.FindFirstValue("Provider") ?? "Email",
            ProfilePhoto = profile.ProfilePhoto,
            IsSubscribed = isSubscribed,
            IsEmailConfirmed = profile.IsEmailConfirmed
        };
        return View(vm);
    }

    // ── GET /Mobile/EditProfile ───────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/EditProfile"));

        var userId = CurrentUserId ?? Guid.Empty;
        var profile = await _profile.GetProfileAsync(userId);
        if (profile is null) return NotFound();

        var vm = new Areas.Mobile.ViewModels.MobileEditProfileViewModel
        {
            Name = profile.Name,
            Email = profile.Email,
            ProfilePhoto = profile.ProfilePhoto,
            IsEmailConfirmed = profile.IsEmailConfirmed
        };
        return View(vm);
    }

    // ── POST /Mobile/EditProfile ──────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> EditProfile(Areas.Mobile.ViewModels.MobileEditProfileViewModel vm)
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/EditProfile"));

        var userId = CurrentUserId ?? Guid.Empty;

        if (!ModelState.IsValid)
        {
            // Repopulate display-only fields from DB
            var currentProfile = await _profile.GetProfileAsync(userId);
            if (currentProfile is not null)
            {
                vm.ProfilePhoto = currentProfile.ProfilePhoto;
                vm.Email = currentProfile.Email;
                vm.IsEmailConfirmed = currentProfile.IsEmailConfirmed;
            }
            TempData["ErrorMessage"] = "Please fix the errors below.";
            return View(vm);
        }

        try
        {
            var editVm = new ViewModels.Profile.EditProfileViewModel
            {
                Name = vm.Name,
                ProfilePhoto = vm.ProfilePhotoFile
            };
            await _profile.UpdateProfileAsync(userId, editVm);

            // Handle optional password change
            if (!string.IsNullOrWhiteSpace(vm.CurrentPassword)
                && !string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                await _profile.ChangePasswordAsync(userId, new ViewModels.Profile.ChangePasswordViewModel
                {
                    CurrentPassword = vm.CurrentPassword,
                    NewPassword = vm.NewPassword,
                    ConfirmPassword = vm.NewPassword
                });
            }

            // Re-sign-in to refresh claims
            var profile = await _profile.GetProfileAsync(userId);
            if (profile is not null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, profile.Id.ToString()),
                    new(ClaimTypes.Name,           profile.Name),
                    new(ClaimTypes.Email,          profile.Email),
                    new(ClaimTypes.Role,           profile.Role),
                    new("ProfilePhoto",            profile.ProfilePhoto ?? "")
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

        return RedirectToAction(nameof(EditProfile));
    }

    // ... rest of the code remains the same ...
    public async Task<IActionResult> AuthorProfile([FromQuery] Guid authorUserId)
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin($"/Mobile/AuthorProfile?authorUserId={authorUserId}"));

        var userId = CurrentUserId ?? Guid.Empty;

        // Get author profile from post service
        var postService = HttpContext.RequestServices.GetRequiredService<ReadEase.Services.Interfaces.IPostService>();
        var vm = await postService.GetAuthorProfileAsync(authorUserId, userId);

        if (vm is null) return NotFound();

        return View(vm);
    }

    // ── GET /Mobile/Archive ───────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Archive()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Archive"));

        var userId = CurrentUserId ?? Guid.Empty;
        var items = await _author.GetArchiveAsync(userId);
        return View(items);
    }

    // ── POST /Mobile/Restore ──────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreBook(Guid id)
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Archive"));

        var userId = CurrentUserId ?? Guid.Empty;
        try
        {
            await _author.RestoreBookAsync(id, userId);
            TempData["SuccessMessage"] = "✅ Book restored successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Archive));
    }

    // ── POST /Mobile/PermanentDelete ──────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDelete(Guid id)
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Archive"));

        var userId = CurrentUserId ?? Guid.Empty;
        try
        {
            await _author.PermanentDeleteAsync(id, userId);
            TempData["SuccessMessage"] = "🗑️ Book permanently deleted.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Archive));
    }

    // ── GET /Mobile/Favorites ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Favorites()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Favorites"));

        var userId = CurrentUserId ?? Guid.Empty;
        var favorites = await _library.GetFavoritesAsync(userId);
        var bookIds = favorites.Select(f => f.Book.Id).ToList();
        var readCounts = await _library.GetReadCountsBatchAsync(bookIds);

        var vm = new ReadEase.ViewModels.Library.FavoritesViewModel
        {
            Books = favorites.Select(f => new ReadEase.ViewModels.Library.BookCardViewModel
            {
                Id = f.Book.Id,
                Title = f.Book.Title,
                Author = f.Book.Author,
                Category = f.Book.Category,
                CoverImage = f.Book.CoverImage,
                IsFree = f.Book.IsFree,
                IsFavorite = true,
                ReadCount = readCounts.GetValueOrDefault(f.Book.Id, 0)
            }).ToList(),
            IsSubscribed = await GetIsSubscribedAsync()
        };
        return View(vm);
    }

    // ── GET /Mobile/Subscribe ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Subscribe()
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Subscribe"));

        var userId = CurrentUserId ?? Guid.Empty;

        // FIX: read both the latest request AND the actual current tier from DB
        var request = await _subscription.GetStatusAsync(userId);
        var currentTier = await _subscription.GetCurrentTierAsync(userId);

        // Staff always treated as PremiumPlus
        if (User.IsInRole("Admin") || User.IsInRole("Author"))
            currentTier = SubscriptionTier.PremiumPlus;

        var vm = new ReadEase.ViewModels.Subscribe.SubscribeViewModel
        {
            IsSubscribed = currentTier >= SubscriptionTier.Premium,
            CurrentTier = currentTier,                                   // FIX: was always Free
            RequestedTier = request?.RequestedTier ?? SubscriptionTier.Premium,
            CurrentStatus = request?.Status,
            RejectionReason = request?.Reason,
            SubmittedAt = request?.SubmittedAt
        };
        return View(vm);
    }

    // ── POST /Mobile/Subscribe ────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Subscribe(
        IFormFile? receiptPhoto,
        [FromForm] SubscriptionTier requestedTier = SubscriptionTier.Premium)
    {
        if (!await CheckLoginAsync()) return Redirect(RedirectToLogin("/Mobile/Subscribe"));

        var userId = CurrentUserId ?? Guid.Empty;
        string? receiptPath = null;

        if (receiptPhoto is { Length: > 0 })
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads", "receipts");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(receiptPhoto.FileName)}";
            var fullPath = Path.Combine(uploads, fileName);
            await using var stream = System.IO.File.Create(fullPath);
            await receiptPhoto.CopyToAsync(stream);
            receiptPath = $"/uploads/receipts/{fileName}";
        }

        try
        {
            await _subscription.SubmitRequestAsync(userId, receiptPath, requestedTier);
            TempData["SuccessMessage"] = "✅ Receipt submitted! We'll review your subscription within 24 hours.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction("Subscribe");
    }

    // ── External Login (Google via Chrome Custom Tabs) ────────────────────

    [HttpGet("~/Mobile/Auth/ExternalLogin")]
    public IActionResult ExternalLogin(string provider, bool mobile = false)
    {
        if (provider != "Google")
            return BadRequest("Only Google provider is supported");

        // FIX: Use a short-lived cookie instead of Session to track mobile flag.
        // Session is unreliable across OAuth redirects (different domains/ports).
        // SameSite=None is required because Google redirects back from accounts.google.com.
        Response.Cookies.Append("MobileAuthPending", mobile ? "true" : "false", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10)
        });

        // Challenge Google OAuth - will redirect to Google's sign-in page.
        // FIX: Use "Google" as scheme name — must match what AddGoogle() registers in Program.cs.
        // If you used .AddGoogle(o => { o.SignInScheme = "External"; ... }) then use "Google" here.
        var redirectUrl = Url.Action("ExternalLoginCallback", "Mobile", new { area = "Mobile" });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, "Google");
    }

    [HttpGet("~/Mobile/Auth/ExternalLoginCallback")]
    public async Task<IActionResult> ExternalLoginCallback()
    {
        // FIX: Use "External" only if your Program.cs registers AddCookie("External") as the
        // external sign-in scheme. If you use the default, change to "Google" below.
        // Most setups use: services.AddAuthentication().AddCookie("External").AddGoogle(o => o.SignInScheme = "External")
        var result = await HttpContext.AuthenticateAsync("External");

        if (!result.Succeeded || result.Principal == null)
        {
            _logger.LogWarning("Google OAuth callback failed: {Error}", result.Failure?.Message);
            return RedirectToAction("Login", new { error = "Google authentication failed" });
        }

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name);
        var providerId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login", new { error = "Could not get email from Google" });
        }

        try
        {
            // Find or create user via Google auth
            var user = await _auth.FindOrCreateExternalUserAsync("Google", providerId!, email, name ?? email.Split('@')[0]);

            // Generate JWT token
            var token = _tokenService.GenerateJwtToken(user);

            // FIX: Read mobile flag from cookie (replaces unreliable Session)
            var isMobile = Request.Cookies["MobileAuthPending"] == "true";
            Response.Cookies.Delete("MobileAuthPending");

            if (isMobile)
            {
                // Redirect to MAUI app via deep link — the app stores the token in SecureStorage
                var deepLink = $"readease://auth/callback?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
                return Redirect(deepLink);
            }

            // Set cookie for web browser flow
            Response.Cookies.Append("MobileAuthToken", token, new CookieOptions
            {
                HttpOnly = false,   // Must be readable by JS (mobileApi reads it via document.cookie)
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(30)
            });
            Response.Cookies.Append("MobileAuth", "true", new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(30)
            });

            return RedirectToAction("Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Google auth callback");
            return RedirectToAction("Login", new { error = ex.Message });
        }
    }
}