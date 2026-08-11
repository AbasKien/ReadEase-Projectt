using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.DTOs.Book;
using ReadEase.DTOs.Bookdtos;
using ReadEase.Enums;
using ReadEase.Helpers;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Library;
using System.Security.Claims;

namespace ReadEase.Controllers;

[Authorize]
public class LibraryController : Controller
{
    private readonly ILibraryService _library;
    private readonly IChapterService _chapterService;
    private readonly IAuditService _audit;
    private readonly IBookReadRepository _readRepo;

    public LibraryController(
        ILibraryService library,
        IChapterService chapterService,
        IAuditService audit,
        IBookReadRepository readRepo)
    {
        _library = library;
        _chapterService = chapterService;
        _audit = audit;
        _readRepo = readRepo;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
    private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? "User";
    private bool IsAdmin() => GetRole() == nameof(UserRole.Admin);
    private async Task<bool> IsPremium(Guid userId)
        => IsAdmin() || await _library.IsSubscribedAsync(userId);
    private async Task<bool> IsPremiumPlus(Guid userId)
        => IsAdmin() || await _library.IsPremiumPlusAsync(userId);

    // ── GET /Library ──────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? category, string? author)
    {
        var userId = GetUserId();
        var premium = await IsPremium(userId);

        var effectiveSearch = !string.IsNullOrWhiteSpace(author) ? author : search;
        var searchIsAuthor = !string.IsNullOrWhiteSpace(author);

        var (books, favoriteIds) = await _library.GetLibraryAsync(userId, premium, effectiveSearch, category);

        if (searchIsAuthor)
            books = books.Where(b => b.Author.Equals(author, StringComparison.OrdinalIgnoreCase)).ToList();

        var categories = await _library.GetCategoriesAsync();
        var bookIds = books.Select(b => b.Id).ToList();
        var readCounts = await _library.GetReadCountsBatchAsync(bookIds);

        // ── Featured sections (only on main library page, not during search/filter) ──
        var topPicks = new List<BookCardViewModel>();
        var newReleases = new List<BookCardViewModel>();
        var recommended = new List<BookCardViewModel>();

        if (string.IsNullOrEmpty(search) && string.IsNullOrEmpty(category) && string.IsNullOrEmpty(author))
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
                Description = b.Description,
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
                Description = b.Description,
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
                Description = b.Description,
                IsFree = b.IsFree,
                IsFavorite = favoriteIds.Contains(b.Id),
                ReadCount = featuredReadCounts.GetValueOrDefault(b.Id, 0)
            }).ToList();
        }

        var vm = new LibraryViewModel
        {
            Books = books.Select(b => new BookCardViewModel
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category,
                CoverImage = b.CoverImage,
                Description = b.Description,
                IsFree = b.IsFree,
                IsFavorite = favoriteIds.Contains(b.Id),
                ReadCount = readCounts.GetValueOrDefault(b.Id, 0)
            }).ToList(),
            Categories = categories,
            SearchQuery = searchIsAuthor ? null : search,
            SelectedCategory = category,
            IsSubscribed = premium,
            IsAdmin = IsAdmin(),
            TopPicks = topPicks,
            NewReleases = newReleases,
            RecommendedForYou = recommended
        };

        ViewBag.AuthorFilter = author;
        return View(vm);
    }

    // ── GET /Library/Read/{id}?chapter=N ─────────────────────────────────
    /// <summary>
    /// Tier read-gate rules:
    ///
    ///   Admin / Book Author  → bypass everything, sees scheduled chapters
    ///   Premium++            → reads immediately, no delay
    ///   Premium              → can read 24 hrs BEFORE free unlock
    ///                          (i.e. from PremiumUnlocksAt onward)
    ///   Free                 → reads from ScheduledAt / PublishedAt onward
    ///
    /// Example: author sets free release = March 30 08:00
    ///   PremiumUnlocksAt = March 29 08:00  ← premium can read from here
    ///   ScheduledAt      = March 30 08:00  ← free can read from here
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Read(Guid id, int? chapter)
    {
        var userId = GetUserId();
        var role = GetRole();
        var isAdmin = IsAdmin();

        var premiumPlus = await IsPremiumPlus(userId);
        var premium = premiumPlus || await IsPremium(userId);

        var book = await _library.GetBookByIdAsync(id);
        var isBookAuthor = book is not null && book.PublishedByUserId == userId;
        var bypassGate = isAdmin || isBookAuthor;

        try
        {
            var vm = await _library.OpenBookAsync(userId, id, premium, role);
            if (vm is null) return NotFound();

            var chapterData = await _chapterService.GetReaderDataAsync(
                id, chapter,
                isPremium: premium,
                isPremiumPlus: premiumPlus,
                bypassGate: bypassGate);

            if (chapterData is not null)
            {
                // ── Tier read-gate ────────────────────────────────────────
                // Premium++, admin, and book author bypass the gate entirely.
                if (!bypassGate && !premiumPlus)
                {
                    var now = DateTime.UtcNow;

                    if (premium)
                    {
                        // Premium: can read from PremiumUnlocksAt (24 hrs before free)
                        // If PremiumUnlocksAt is null the chapter was published immediately → no gate.
                        if (chapterData.PremiumUnlocksAt.HasValue && now < chapterData.PremiumUnlocksAt.Value)
                        {
                            var unlocksIn = chapterData.PremiumUnlocksAt.Value - now;
                            var unlocksMsg = FormatTimeLeft(unlocksIn);
                            TempData["ErrorMessage"] =
                                $"⏳ This chapter unlocks for Premium readers in {unlocksMsg}.";
                            return RedirectToAction("Detail", new { id });
                        }
                    }
                    else
                    {
                        // Free: can read from PublishedAt (= ScheduledAt when chapter goes fully live)
                        var publishedAt = chapterData.PublishedAt;
                        if (now < publishedAt)
                        {
                            var unlocksIn = publishedAt - now;
                            var unlocksMsg = FormatTimeLeft(unlocksIn);
                            TempData["ErrorMessage"] =
                                $"⏳ This chapter unlocks in {unlocksMsg}. " +
                                $"Upgrade to Premium to read 24 hrs early, or Premium++ to read instantly!";
                            return RedirectToAction("Detail", new { id });
                        }
                    }
                }

                vm.CurrentChapter = chapterData.CurrentChapter;
                vm.Content = chapterData.Content;
                vm.Chapters = chapterData.AllChapters.Select(c => new ChapterNavItem
                {
                    Id = c.Id,
                    ChapterNumber = c.ChapterNumber,
                    Title = c.Title,
                    IsVisible = c.IsVisible,
                    ScheduledAt = c.ScheduledAt,
                    PremiumUnlocksAt = c.PremiumUnlocksAt
                }).ToList();

                await _readRepo.LogChapterAsync(userId, id, chapterData.CurrentChapter);
            }

            vm.IsCompleted = await _library.IsBookCompletedAsync(userId, id);

            // ── Tier flags for the reader view's chapter nav lock UI ──────────
            // Mirrors the same ViewBag set in LibraryController.Detail so that
            // any chapter lock rendering in the reader sidebar works correctly.
            ViewBag.IsPremiumPlus = premiumPlus;
            ViewBag.IsPremium = premium && !premiumPlus;
            ViewBag.IsBypassGate = bypassGate;

            await _audit.LogAsync(userId, role, "BookOpened", $"BookId:{id}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());

            return View(vm);
        }
        catch (UnauthorizedAccessException)
        {
            TempData["ErrorMessage"] = "This book requires a Premium subscription.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ── GET /Library/Detail/{id} ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Detail(Guid id)
    {
        var userId = GetUserId();
        var premium = await IsPremium(userId);
        var premiumPlus = await IsPremiumPlus(userId);
        var isAdmin = IsAdmin();

        var vm = await _library.GetBookDetailAsync(userId, id, premium, isAdmin, premiumPlus);
        if (vm is null) return NotFound();

        // ── Enrich chapter list with unlock timestamps ─────────────────────
        // We pass the tier flags so ChapterService returns scheduled (locked)
        // chapters to premium/premium++ users — they can see them but not read yet.
        var book = await _library.GetBookByIdAsync(id);
        var isBookAuthor = book is not null && book.PublishedByUserId == userId;
        var bypassGate = isAdmin || isBookAuthor;

        var chapterData = await _chapterService.GetReaderDataAsync(
            id, requestedChapter: null,
            isPremium: premium,
            isPremiumPlus: premiumPlus,
            bypassGate: bypassGate);

        if (chapterData is not null)
        {
            vm.Chapters = chapterData.AllChapters.Select(c => new ChapterNavItem
            {
                Id = c.Id,
                ChapterNumber = c.ChapterNumber,
                Title = c.Title,
                IsVisible = c.IsVisible,
                ScheduledAt = c.ScheduledAt,
                PremiumUnlocksAt = c.PremiumUnlocksAt
            }).ToList();
        }

        // Pass tier info to the view via ViewBag so Detail.cshtml can render
        // the correct unlock message per chapter without extra round-trips.
        ViewBag.IsPremiumPlus = premiumPlus;
        ViewBag.IsPremium = premium && !premiumPlus;
        ViewBag.IsBypassGate = bypassGate;

        return View(vm);
    }

    // ── GET /Library/GetTopPicks ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetTopPicks()
    {
        var picks = await _library.GetTopPicksAsync();
        return Json(picks.Select(p => new
        {
            bookId = p.BookId,
            title = p.Title,
            author = p.Author,
            coverImage = p.CoverImage,
            avgRating = p.AvgRating,
            ratingCount = p.RatingCount,
            isFree = p.IsFree,
            url = Url.Action("Detail", "Library", new { id = p.BookId })
        }));
    }

    // ── GET /Library/SearchJson ───────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> SearchJson(string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(new { books = Array.Empty<object>(), authors = Array.Empty<object>() });

        var userId = GetUserId();
        var premium = await IsPremium(userId);
        var ql = q.ToLower();

        var (allBooks, _) = await _library.GetLibraryAsync(userId, premium, null, null);

        var bookResults = allBooks
            .Where(b => b.Title.ToLower().Contains(ql))
            .Select(b => new
            {
                title = b.Title,
                author = b.Author,
                category = b.Category,
                coverImage = b.CoverImage,
                url = Url.Action("Detail", "Library", new { id = b.Id })
            })
            .Take(5).ToList<object>();

        var authorResults = allBooks
            .GroupBy(b => b.Author, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key != null && g.Key.ToLower().Contains(ql))
            .Take(3)
            .Select(g => new
            {
                name = g.Key,
                books = g.Select(b => new
                {
                    title = b.Title,
                    category = b.Category,
                    coverImage = b.CoverImage,
                    url = Url.Action("Detail", "Library", new { id = b.Id })
                }).ToList<object>(),
                url = Url.Action("Index", "Library", new { search = g.Key })
            })
            .ToList<object>();

        return Json(new { books = bookResults, authors = authorResults });
    }

    // ── POST /Library/GetReadCounts ───────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GetReadCounts([FromBody] List<Guid> bookIds)
    {
        if (bookIds is null || bookIds.Count == 0)
            return Json(new Dictionary<string, int>());

        var counts = await _library.GetReadCountsBatchAsync(bookIds);
        return Json(counts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));
    }

    // ── POST /Library/SaveScroll ──────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> SaveScroll([FromBody] SaveScrollRequestDto req)
    {
        await _library.SaveScrollAsync(GetUserId(), req.BookId, req.ScrollPosition);
        return Ok();
    }

    // ── GET /api/library/categories ─────────────────────────────────────────
    [HttpGet("/api/library/categories")]
    public async Task<IActionResult> GetCategoriesJson()
    {
        var categories = await _library.GetCategoriesAsync();
        return Json(categories);
    }

    // ── GET /Library/Favorites ────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Favorites()
    {
        var userId = GetUserId();
        var favorites = await _library.GetFavoritesAsync(userId);
        var bookIds = favorites.Select(f => f.Book.Id).ToList();
        var readCounts = await _library.GetReadCountsBatchAsync(bookIds);

        var vm = new FavoritesViewModel
        {
            Books = favorites.Select(f => new BookCardViewModel
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
            IsSubscribed = await IsPremium(userId)
        };
        return View(vm);
    }

    // ── POST /Library/AddReview ───────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> AddReview([FromBody] AddReviewRequestDto req)
    {
        if (req.Stars < 1 || req.Stars > 5)
            return Json(new AddReviewResponseDto { Success = false, Error = "Invalid star rating." });

        var result = await _library.AddReviewAsync(GetUserId(), req.BookId, req.Stars, req.Text);
        return Json(result);
    }

    // ── GET /Library/GetReviews ───────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetReviews(Guid bookId)
    {
        var result = await _library.GetReviewsAsync(GetUserId(), bookId);
        return Json(result);
    }

    // ── GET /Library/GetReadingCounts ─────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetReadingCounts()
    {
        var userId = GetUserId();
        var ongoing = await _library.GetOngoingCountAsync(userId);
        var completed = await _library.GetCompletedCountAsync(userId);
        return Json(new { ongoing, completed });
    }

    // ── GET /Library/Ongoing ──────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Ongoing()
    {
        var userId = GetUserId();
        var premium = await IsPremium(userId);
        var vm = await _library.GetOngoingBooksAsync(userId, premium);
        return View(vm);
    }

    // ── GET /Library/Completed ────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Completed()
    {
        var userId = GetUserId();
        var premium = await IsPremium(userId);
        var vm = await _library.GetCompletedBooksAsync(userId, premium);
        return View(vm);
    }

    // ── GET /Library/DownloadBook/{id} ────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> DownloadBook(Guid id)
    {
        var userId = GetUserId();
        var premiumPlus = await IsPremiumPlus(userId);

        var book = await _library.GetBookByIdAsync(id);
        if (book is null) return NotFound();

        if (!premiumPlus)
        {
            TempData["ErrorMessage"] = "⬇️ Downloads require a Premium++ subscription.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var chapterData = await _chapterService.GetReaderDataAsync(id, null,
            isPremiumPlus: true, bypassGate: true);
        var chapters = chapterData?.AllChapters?
            .OrderBy(c => c.ChapterNumber)
            .Select(c => (c.ChapterNumber, c.Title, c.Content ?? string.Empty))
            .ToList();

        var html = BookDownloadHelper.BuildBookHtml(book.Title, book.Author, book.Category, book.Description, chapters, book.Content);
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
        var fileName = BookDownloadHelper.SanitizeFileName(book.Title) + ".html";
        return File(bytes, "text/html; charset=utf-8", fileName);
    }

    // ── GET /Library/DownloadChapter ─────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> DownloadChapter(Guid chapterId, Guid bookId)
    {
        var userId = GetUserId();
        var premiumPlus = await IsPremiumPlus(userId);

        var book = await _library.GetBookByIdAsync(bookId);
        if (book is null) return NotFound();

        if (!premiumPlus)
        {
            TempData["ErrorMessage"] = "⬇️ Downloads require a Premium++ subscription.";
            return RedirectToAction(nameof(Detail), new { id = bookId });
        }

        var chapterData = await _chapterService.GetReaderDataAsync(bookId, null,
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
        return File(bytes, "text/html; charset=utf-8", fileName);
    }

    // ── POST /Library/AddComment ──────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] AddCommentRequestDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return Json(new { success = false, error = "Comment cannot be empty." });

        var (success, error, comment) = await _library.AddCommentAsync(
            GetUserId(), req.ReviewId, req.Text);

        return Json(new { success, error, comment });
    }

    // ── POST /Library/AddCommentReply ─────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> AddCommentReply([FromBody] AddCommentReplyDto req)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return Json(new { success = false, error = "Reply cannot be empty." });

        var (success, error, reply) = await _library.AddCommentReplyAsync(
            GetUserId(), req.ReviewId, req.CommentId, req.Text,
            req.ReplyingTo, req.ParentReplyId);

        return Json(new { success, error, reply });
    }

    // ── POST /Library/ToggleLike ──────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ToggleLike([FromBody] Guid reviewId)
    {
        var (liked, count) = await _library.ToggleLikeAsync(GetUserId(), reviewId);
        return Json(new { liked, count });
    }

    // ── POST /Library/ToggleCommentLike ───────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ToggleCommentLike([FromBody] Guid commentId)
    {
        var (liked, count) = await _library.ToggleCommentLikeAsync(GetUserId(), commentId);
        return Json(new { liked, count });
    }

    // ── POST /Library/ReplyToReview ───────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ReplyToReview([FromBody] ReplyToReviewRequestDto req)
    {
        var (success, error) = await _library.ReplyToReviewAsync(
            GetUserId(), req.ReviewId, req.ReplyText);
        return Json(new { success, error });
    }

    // ── POST /Library/DeleteReply ─────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> DeleteReply([FromBody] Guid reviewId)
    {
        var (success, error) = await _library.DeleteReplyAsync(GetUserId(), reviewId);
        return Json(new { success, error });
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static string FormatTimeLeft(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours}h";
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        return $"{ts.Minutes}m";
    }
}