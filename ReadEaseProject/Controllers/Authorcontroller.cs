using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.DTOs.Bookdtos;
using ReadEase.Jobs;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Author;
using System.Security.Claims;

namespace ReadEase.Controllers;

[Authorize]
public class AuthorController : Controller
{
    private readonly IAuthorService _authorService;
    private readonly IChapterService _chapterService;
    private readonly IAuditService _audit;
    private readonly IWebHostEnvironment _env;

    public AuthorController(
        IAuthorService authorService,
        IChapterService chapterService,
        IAuditService audit,
        IWebHostEnvironment env)
    {
        _authorService = authorService;
        _chapterService = chapterService;
        _audit = audit;
        _env = env;
    }

    // ── GET /Author/Dashboard ─────────────────────────────────────────────
    [HttpGet, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> Dashboard()
    {
        var vm = await _authorService.GetDashboardAsync(
            GetUserId(),
            User.Identity?.Name ?? "Author",
            User.FindFirstValue(ClaimTypes.Email) ?? "");
        return View("~/Views/Author/AuthorDashboard.cshtml", vm);
    }

    // ── GET /Author/MyBooks ───────────────────────────────────────────────
    [HttpGet, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> MyBooks()
    {
        var vm = await _authorService.GetMyBooksAsync(GetUserId());
        return View("~/Views/Author/MyBooks.cshtml", vm);
    }

    // ── GET /Author/Create ────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Create()
        => View("~/Views/Author/PublishBook.cshtml", new PublishBookViewModel());

    // ── POST /Author/Create ───────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PublishBookViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Author/PublishBook.cshtml", vm);

        var userId = GetUserId();
        var (isPending, wasPromoted) = await _authorService.PublishBookAsync(
            userId, User.Identity!.Name ?? "Unknown", vm, _env.WebRootPath);

        await _audit.LogAsync(userId, wasPromoted ? "Author" : GetRole(),
            "BookPublished", $"Title:{vm.Title} IsFree:{vm.IsFree} Pending:{isPending}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers["User-Agent"].ToString());

        if (wasPromoted)
            await RefreshAuthCookieAsync("Author");

        TempData["SuccessMessage"] = (wasPromoted, isPending) switch
        {
            (true, true) => $"🎉 You're now an Author! \"{vm.Title}\" is pending admin approval.",
            (true, false) => $"🎉 You're now an Author! \"{vm.Title}\" is live in the library!",
            (false, true) => $"📋 \"{vm.Title}\" submitted for admin approval.",
            _ => $"✅ \"{vm.Title}\" published successfully!"
        };

        return RedirectToAction(nameof(MyBooks));
    }

    // ── GET /Author/Edit/{id} ─────────────────────────────────────────────
    [HttpGet, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var vm = await _authorService.GetBookForEditAsync(id, GetUserId());
        if (vm is null) return Forbid();
        return View("~/Views/Author/PublishBook.cshtml", vm);
    }

    // ── POST /Author/Edit ─────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> Edit(PublishBookViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Author/PublishBook.cshtml", vm);

        var isPending = await _authorService.EditBookAsync(GetUserId(), vm, _env.WebRootPath);

        await _audit.LogAsync(GetUserId(), "Author", "BookEdited",
            $"BookId:{vm.Id} IsFree:{vm.IsFree} Pending:{isPending}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers["User-Agent"].ToString());

        TempData["SuccessMessage"] = isPending
            ? $"📋 \"{vm.Title}\" has been submitted for admin approval to become a Premium book."
            : $"✅ \"{vm.Title}\" updated successfully!";

        return RedirectToAction(nameof(MyBooks));
    }

    // ── POST /Author/Delete ───────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _authorService.ArchiveBookAsync(id, GetUserId());
        await _audit.LogAsync(GetUserId(), "Author", "BookArchived", $"BookId:{id}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers["User-Agent"].ToString());
        TempData["SuccessMessage"] = "Book moved to archive.";
        return RedirectToAction(nameof(MyBooks));
    }

    // ── GET /Author/Archive ───────────────────────────────────────────────
    [HttpGet, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> Archive()
    {
        var items = await _authorService.GetArchiveAsync(GetUserId());
        return View("~/Views/Author/Archive.cshtml", items);
    }

    // ── POST /Author/Restore ──────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> Restore(Guid id)
    {
        await _authorService.RestoreBookAsync(id, GetUserId());
        await _audit.LogAsync(GetUserId(), "Author", "BookRestored", $"BookId:{id}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers["User-Agent"].ToString());
        TempData["SuccessMessage"] = "✅ Book restored successfully!";
        return RedirectToAction(nameof(Archive));
    }

    // ── POST /Author/PermanentDelete ──────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> PermanentDelete(Guid id)
    {
        await _authorService.PermanentDeleteAsync(id, GetUserId());
        await _audit.LogAsync(GetUserId(), "Author", "BookPermanentlyDeleted", $"BookId:{id}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers["User-Agent"].ToString());
        TempData["SuccessMessage"] = "Book permanently deleted.";
        return RedirectToAction(nameof(Archive));
    }

    // ════════════════════════════════════════════════════════════════
    //  CHAPTER ACTIONS
    // ════════════════════════════════════════════════════════════════

    // ── GET /Author/Chapters ──────────────────────────────────────────────
    [HttpGet, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> Chapters(Guid bookId)
    {
        try
        {
            var dto = await _chapterService.GetManageViewAsync(bookId, GetUserId());
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
                    PublishedAt = c.PublishedAt,
                    ScheduledAt = c.ScheduledAt,   // ← new
                    IsVisible = c.IsVisible       // ← new
                }).ToList()
            };
            return View("~/Views/Author/ManageChapters.cshtml", vm);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── GET /Author/AddChapter ────────────────────────────────────────────
    [HttpGet, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> AddChapter(Guid bookId)
    {
        try
        {
            var vm = await _chapterService.GetAddFormAsync(bookId, GetUserId());
            return View("~/Views/Author/PublishChapter.cshtml", vm);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── POST /Author/AddChapter ───────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> AddChapter(PublishChapterViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Author/PublishChapter.cshtml", vm);

        try
        {
            var dto = new PublishChapterDto
            {
                BookId = vm.BookId,
                Title = vm.Title,
                Content = vm.Content,
                // ScheduledAt = the FREE reader unlock time (as entered by the author)
                ScheduledAt = vm.ScheduledAt.HasValue
                                  ? DateTime.SpecifyKind(vm.ScheduledAt.Value, DateTimeKind.Local)
                                        .ToUniversalTime()
                                  : null
            };

            var (chapterNum, chapterId) = await _chapterService.PublishChapterAsync(
                dto, GetUserId(), User.Identity!.Name ?? "Unknown");

            // ── Decide: schedule or publish now ──────────────────────────────
            if (dto.ScheduledAt.HasValue && dto.ScheduledAt.Value > DateTime.UtcNow)
            {
                // ── Job 1: Flip chapter visible at the FREE unlock time ──────
                var freeDelay = dto.ScheduledAt.Value - DateTime.UtcNow;
                var freeJobId = BackgroundJob.Schedule<ChapterReleaseJob>(
                    job => job.ExecuteAsync(
                        chapterId, vm.BookId, vm.BookTitle, chapterNum, vm.Title),
                    freeDelay);

                // Persist the main job ID (used to cancel if author deletes the chapter)
                await _chapterService.SaveJobIdAsync(chapterId, freeJobId);

                // ── Job 2: Notify PREMIUM fans 24 hrs before free unlock ─────
                // PremiumUnlocksAt = ScheduledAt - 24 hours
                var premiumUnlocksAt = dto.ScheduledAt.Value.AddHours(-24);
                if (premiumUnlocksAt > DateTime.UtcNow)
                {
                    var premiumDelay = premiumUnlocksAt - DateTime.UtcNow;
                    BackgroundJob.Schedule<ChapterPremiumNotifyJob>(
                        job => job.ExecuteAsync(
                            chapterId, vm.BookId, vm.BookTitle, chapterNum, vm.Title),
                        premiumDelay);
                }
                // If premiumUnlocksAt is already in the past (e.g. author scheduled
                // less than 24 hrs ahead), skip the premium notification job —
                // the chapter is already within the premium window.

                await _audit.LogAsync(GetUserId(), "Author", "ChapterScheduled",
                    $"BookId:{vm.BookId} Chapter:{chapterNum} ScheduledAt:{dto.ScheduledAt:u} JobId:{freeJobId}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString());

                TempData["SuccessMessage"] =
                    $"📅 Chapter {chapterNum}: \"{vm.Title}\" scheduled for " +
                    $"{vm.ScheduledAt!.Value:MMM d, yyyy h:mm tt}! " +
                    $"Premium readers get early access 24 hrs before.";
            }
            else
            {
                // Immediate publish — ChapterService already notified all fans
                await _audit.LogAsync(GetUserId(), "Author", "ChapterPublished",
                    $"BookId:{vm.BookId} Chapter:{chapterNum}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString());

                TempData["SuccessMessage"] =
                    $"✅ Chapter {chapterNum}: \"{vm.Title}\" published!";
            }

            return RedirectToAction(nameof(Chapters), new { bookId = vm.BookId });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }


    // ── GET /Author/EditChapter ───────────────────────────────────────────
    [HttpGet, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> EditChapter(Guid id)
    {
        try
        {
            var vm = await _chapterService.GetEditFormAsync(id, GetUserId());
            if (vm is null) return NotFound();
            return View("~/Views/Author/PublishChapter.cshtml", vm);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── POST /Author/EditChapter ──────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> EditChapter(EditChapterViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Author/PublishChapter.cshtml", vm);
        try
        {
            await _chapterService.EditChapterAsync(
                new EditChapterDto { ChapterId = vm.Id, Title = vm.Title, Content = vm.Content },
                GetUserId());

            await _audit.LogAsync(GetUserId(), "Author", "ChapterEdited",
                $"ChapterId:{vm.Id}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());

            TempData["SuccessMessage"] = $"✅ Chapter {vm.ChapterNumber} updated.";
            return RedirectToAction(nameof(Chapters), new { bookId = vm.BookId });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── POST /Author/DeleteChapter ────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> DeleteChapter(Guid id, Guid bookId)
    {
        try
        {
            await _chapterService.DeleteChapterAsync(id, GetUserId());
            await _audit.LogAsync(GetUserId(), "Author", "ChapterDeleted",
                $"ChapterId:{id}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());
            TempData["SuccessMessage"] = "Chapter removed.";
            return RedirectToAction(nameof(Chapters), new { bookId });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── POST /Author/ToggleStoryComplete ──────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Author,Admin")]
    public async Task<IActionResult> ToggleStoryComplete(Guid bookId)
    {
        try
        {
            var isComplete = await _authorService.ToggleStoryCompleteAsync(bookId, GetUserId());
            await _audit.LogAsync(GetUserId(), "Author", "StoryCompleteToggled",
                $"BookId:{bookId} IsComplete:{isComplete}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());
            TempData["SuccessMessage"] = isComplete
                ? "✅ Story marked as complete!"
                : "📝 Story marked as ongoing.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Chapters), new { bookId });
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    private string GetRole()
        => User.FindFirstValue(ClaimTypes.Role) ?? "User";

    private async Task RefreshAuthCookieAsync(string newRole)
    {
        var claims = User.Claims.Where(c => c.Type != ClaimTypes.Role).ToList();
        claims.Add(new Claim(ClaimTypes.Role, newRole));
        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });
    }
}