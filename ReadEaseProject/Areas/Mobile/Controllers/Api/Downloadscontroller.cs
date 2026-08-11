using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ReadEase.Helpers;
using ReadEase.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ReadEase.Areas.Mobile.Controllers.Api;

/// <summary>
/// API for downloading chapters for offline reading in MAUI app.
/// </summary>
[Area("Mobile")]
[Route("Areas/Mobile/api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class DownloadsController : ControllerBase
{
    private readonly IChapterService _chapters;
    private readonly ILibraryService _library;
    private readonly IConfiguration _config;
    private readonly ILogger<DownloadsController> _logger;

    public DownloadsController(
        IChapterService chapters,
        ILibraryService library,
        IConfiguration config,
        ILogger<DownloadsController> logger)
    {
        _chapters = chapters;
        _library = library;
        _config = config;
        _logger = logger;
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // -------------------------------------------------------------------------
    // Auth helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validates a raw JWT string using the same signing key as the app.
    /// Used by the browser-download endpoints that receive the token in the URL
    /// query string because Chrome cannot add an Authorization header.
    /// </summary>
    private ClaimsPrincipal? ValidateQueryToken(string token)
    {
        try
        {
            // Try the common config keys your project might use
            var jwtKey = _config["Jwt:Key"]
                      ?? _config["JwtSettings:SecretKey"]
                      ?? _config["Jwt:Secret"]
                      ?? throw new InvalidOperationException("JWT signing key not found in configuration.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, parameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DownloadsController] Query-string token validation failed.");
            return null;
        }
    }

    /// <summary>
    /// Resolves the authenticated user ID from:
    ///   1. A ?token= query-string JWT  (Chrome download — no Authorization header)
    ///   2. The Bearer token validated by [Authorize]  (MAUI HttpClient calls)
    /// Returns (Guid.Empty, false) when neither source yields a valid identity.
    /// </summary>
    private (Guid UserId, bool IsAdmin) ResolveIdentity(string? queryToken)
    {
        ClaimsPrincipal? principal = null;

        if (!string.IsNullOrWhiteSpace(queryToken))
            principal = ValidateQueryToken(queryToken);

        if (principal == null && User.Identity?.IsAuthenticated == true)
            principal = User;

        if (principal == null) return (Guid.Empty, false);

        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId)) return (Guid.Empty, false);

        var isAdmin = principal.IsInRole("Admin") || principal.IsInRole("Author");
        return (userId, isAdmin);
    }

    // -------------------------------------------------------------------------
    // JSON endpoints — still require Bearer header (MAUI HttpClient calls)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get all downloadable chapters for a book (for PremiumPlus users).
    /// Returns chapter metadata + content for local storage.
    /// </summary>
    [HttpGet("book/{bookId:guid}")]
    public async Task<IActionResult> GetBookChapters(Guid bookId)
    {
        try
        {
            var isPremiumPlus = await _library.IsPremiumPlusAsync(UserId);
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Author");
            if (!isPremiumPlus && !isAdmin)
                return StatusCode(403, new { message = "Downloads require Premium++ subscription." });

            var book = await _library.GetBookByIdAsync(bookId);
            if (book == null)
                return NotFound(new { message = "Book not found." });

            var chapterData = await _chapters.GetReaderDataAsync(
                bookId, null, isPremiumPlus: true, bypassGate: true);

            if (chapterData?.AllChapters == null || !chapterData.AllChapters.Any())
                return NotFound(new { message = "No chapters found for this book." });

            var result = new
            {
                book = new
                {
                    id = book.Id,
                    title = book.Title,
                    author = book.Author,
                    category = book.Category,
                    coverImage = book.CoverImage,
                    description = book.Description,
                    isFree = book.IsFree,
                    isStoryComplete = book.IsStoryComplete
                },
                chapters = chapterData.AllChapters
                    .OrderBy(c => c.ChapterNumber)
                    .Select(c => new
                    {
                        id = c.Id,
                        chapterNumber = c.ChapterNumber,
                        title = c.Title,
                        content = c.Content,
                        publishedAt = c.PublishedAt,
                        isVisible = c.IsVisible
                    })
                    .ToList(),
                downloadedAt = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading book {BookId} for user {UserId}", bookId, UserId);
            return StatusCode(500, new { message = "Failed to download book chapters." });
        }
    }

    /// <summary>
    /// Get a single chapter for download.
    /// </summary>
    [HttpGet("chapter/{chapterId:guid}")]
    public async Task<IActionResult> GetChapter(Guid chapterId, [FromQuery] Guid bookId)
    {
        try
        {
            var isPremiumPlus = await _library.IsPremiumPlusAsync(UserId);
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Author");
            if (!isPremiumPlus && !isAdmin)
                return StatusCode(403, new { message = "Downloads require Premium++ subscription." });

            var book = await _library.GetBookByIdAsync(bookId);
            if (book == null)
                return NotFound(new { message = "Book not found." });

            var chapterData = await _chapters.GetReaderDataAsync(
                bookId, null, isPremiumPlus: true, bypassGate: true);

            var chapter = chapterData?.AllChapters?.FirstOrDefault(c => c.Id == chapterId);
            if (chapter == null)
                return NotFound(new { message = "Chapter not found." });

            var result = new
            {
                book = new
                {
                    id = book.Id,
                    title = book.Title,
                    author = book.Author,
                    coverImage = book.CoverImage
                },
                chapter = new
                {
                    id = chapter.Id,
                    chapterNumber = chapter.ChapterNumber,
                    title = chapter.Title,
                    content = chapter.Content,
                    publishedAt = chapter.PublishedAt
                },
                downloadedAt = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading chapter {ChapterId} for user {UserId}", chapterId, UserId);
            return StatusCode(500, new { message = "Failed to download chapter." });
        }
    }

    /// <summary>
    /// Check if user has Premium++ or is Admin/Author (required for downloads).
    /// </summary>
    [HttpGet("check-eligibility")]
    public async Task<IActionResult> CheckEligibility()
    {
        var isPremiumPlus = await _library.IsPremiumPlusAsync(UserId);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("Author");
        return Ok(new { canDownload = isPremiumPlus || isAdmin });
    }

    // -------------------------------------------------------------------------
    // HTML file-download endpoints
    //
    // [AllowAnonymous] because Chrome cannot send an Authorization header when
    // opening a URL. MAUI appends the JWT as ?token=<jwt> and LaunchChrome()
    // opens it. We validate the token manually via ResolveIdentity().
    //
    // application/octet-stream forces Chrome to download the file instead of
    // rendering the HTML inline in the browser tab.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Download full book as an HTML file for offline reading.
    /// URL pattern: /Areas/Mobile/api/Downloads/book/{bookId}/html?token=JWT
    /// </summary>
    [HttpGet("book/{bookId:guid}/html")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadBookHtml(
        Guid bookId,
        [FromQuery] string? token = null)
    {
        try
        {
            var (userId, isAdmin) = ResolveIdentity(token);
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("[DownloadBookHtml] Unauthenticated request for book {BookId}", bookId);
                return Unauthorized(new { message = "Authentication required. Please log in and try again." });
            }

            var isPremiumPlus = await _library.IsPremiumPlusAsync(userId);
            if (!isPremiumPlus && !isAdmin)
                return StatusCode(403, new { message = "Downloads require Premium++ subscription." });

            var book = await _library.GetBookByIdAsync(bookId);
            if (book == null)
                return NotFound(new { message = "Book not found." });

            var chapterData = await _chapters.GetReaderDataAsync(
                bookId, null, isPremiumPlus: true, bypassGate: true);

            var chapters = chapterData?.AllChapters?
                .OrderBy(c => c.ChapterNumber)
                .Select(c => (c.ChapterNumber, c.Title, c.Content ?? string.Empty))
                .ToList();

            var html = BookDownloadHelper.BuildBookHtml(
                book.Title, book.Author, book.Category, book.Description, chapters, book.Content);

            var bytes = Encoding.UTF8.GetBytes(html);
            var fileName = BookDownloadHelper.SanitizeFileName(book.Title) + ".html";

            _logger.LogInformation("[DownloadBookHtml] User {UserId} downloading book {BookId} as {FileName}",
                userId, bookId, fileName);

            return File(bytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DownloadBookHtml] Error for book {BookId}", bookId);
            return StatusCode(500, new { message = "Failed to download book." });
        }
    }

    /// <summary>
    /// Download a single chapter as an HTML file for offline reading.
    /// URL pattern: /Areas/Mobile/api/Downloads/chapter/{chapterId}/html?bookId=...&amp;token=JWT
    /// </summary>
    [HttpGet("chapter/{chapterId:guid}/html")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadChapterHtml(
        Guid chapterId,
        [FromQuery] Guid bookId,
        [FromQuery] string? token = null)
    {
        try
        {
            var (userId, isAdmin) = ResolveIdentity(token);
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("[DownloadChapterHtml] Unauthenticated request for chapter {ChapterId}", chapterId);
                return Unauthorized(new { message = "Authentication required. Please log in and try again." });
            }

            var isPremiumPlus = await _library.IsPremiumPlusAsync(userId);
            if (!isPremiumPlus && !isAdmin)
                return StatusCode(403, new { message = "Downloads require Premium++ subscription." });

            var book = await _library.GetBookByIdAsync(bookId);
            if (book == null)
                return NotFound(new { message = "Book not found." });

            var chapterData = await _chapters.GetReaderDataAsync(
                bookId, null, isPremiumPlus: true, bypassGate: true);

            var chapter = chapterData?.AllChapters?.FirstOrDefault(c => c.Id == chapterId);
            if (chapter == null)
                return NotFound(new { message = "Chapter not found." });

            var chapters = new List<(int, string, string)>
            {
                (chapter.ChapterNumber, chapter.Title, chapter.Content ?? string.Empty)
            };

            var html = BookDownloadHelper.BuildBookHtml(
                book.Title, book.Author, book.Category, null, chapters, null, singleChapter: true);

            var bytes = Encoding.UTF8.GetBytes(html);
            var fileName = $"{BookDownloadHelper.SanitizeFileName(book.Title)}_Ch{chapter.ChapterNumber}.html";

            _logger.LogInformation("[DownloadChapterHtml] User {UserId} downloading chapter {ChapterId} as {FileName}",
                userId, chapterId, fileName);

            return File(bytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DownloadChapterHtml] Error for chapter {ChapterId}", chapterId);
            return StatusCode(500, new { message = "Failed to download chapter." });
        }
    }
}