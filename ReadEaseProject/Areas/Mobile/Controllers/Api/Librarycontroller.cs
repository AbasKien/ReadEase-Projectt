using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.Services.Interfaces;
using System.Security.Claims;

namespace ReadEase.Areas.Mobile.Controllers.Api;

[Area("Mobile")]
[Route("Areas/Mobile/api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class LibraryController : ControllerBase
{
    private readonly ILibraryService _library;

    public LibraryController(ILibraryService library)
    {
        _library = library;
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsSubscribed =>
        User.IsInRole("Admin") ||
        User.IsInRole("Author") ||
        // You can enrich this from DB if needed; for now role-based
        false;

    // ── GET Areas/Mobile/api/Library ─────────────────────────────────────
    // Returns all books (search + category filter supported)
    [HttpGet]
    public async Task<IActionResult> GetLibrary(
        [FromQuery] string? search,
        [FromQuery] string? category)
    {
        var (books, favoriteIds) =
            await _library.GetLibraryAsync(UserId, IsSubscribed, search, category);

        var result = books.Select(b => new
        {
            b.Id,
            b.Title,
            b.Author,
            b.Category,
            b.CoverImage,
            b.IsFree,
            b.PublishedAt,
            b.IsStoryComplete,
            IsFavorite = favoriteIds.Contains(b.Id)
        });

        return Ok(result);
    }

    // ── GET Areas/Mobile/api/Library/categories ───────────────────────────
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await _library.GetCategoriesAsync();
        return Ok(cats);
    }

    // ── GET Areas/Mobile/api/Library/toppicks ─────────────────────────────
    [HttpGet("toppicks")]
    public async Task<IActionResult> GetTopPicks([FromQuery] int take = 5)
    {
        var picks = await _library.GetTopPicksAsync(take);
        return Ok(picks);
    }

    // ── GET Areas/Mobile/api/Library/detail/{bookId} ──────────────────────
    [HttpGet("detail/{bookId:guid}")]
    public async Task<IActionResult> GetDetail(Guid bookId)
    {
        var isAdmin = User.IsInRole("Admin");
        var vm = await _library.GetBookDetailAsync(UserId, bookId, IsSubscribed, isAdmin);
        if (vm is null) return NotFound(new { message = "Book not found." });
        return Ok(vm);
    }

    // ── GET Areas/Mobile/api/Library/similar/{bookId} ─────────────────────
    [HttpGet("similar/{bookId:guid}")]
    public async Task<IActionResult> GetSimilar(Guid bookId, [FromQuery] int limit = 6)
    {
        var book = await _library.GetBookByIdAsync(bookId);
        if (book is null) return NotFound();
        var similar = await _library.GetSimilarBooksAsync(bookId, book.Category, limit);
        return Ok(similar);
    }

    // ── POST Areas/Mobile/api/Library/favorite/{bookId} ───────────────────
    [HttpPost("favorite/{bookId:guid}")]
    public async Task<IActionResult> ToggleFavorite(Guid bookId)
    {
        var isFav = await _library.ToggleFavoriteAsync(UserId, bookId);
        return Ok(new { isFavorite = isFav });
    }

    // ── GET Areas/Mobile/api/Library/favorites ────────────────────────────
    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites()
    {
        var favs = await _library.GetFavoritesAsync(UserId);
        return Ok(favs);
    }

    // ── GET Areas/Mobile/api/Library/ongoing ──────────────────────────────
    [HttpGet("ongoing")]
    public async Task<IActionResult> GetOngoing()
    {
        var vm = await _library.GetOngoingBooksAsync(UserId, IsSubscribed);
        return Ok(vm);
    }

    // ── GET Areas/Mobile/api/Library/completed ────────────────────────────
    [HttpGet("completed")]
    public async Task<IActionResult> GetCompleted()
    {
        var vm = await _library.GetCompletedBooksAsync(UserId, IsSubscribed);
        return Ok(vm);
    }

    // ── GET Areas/Mobile/api/Library/counts ───────────────────────────────
    [HttpGet("counts")]
    public async Task<IActionResult> GetCounts()
    {
        var ongoing = await _library.GetOngoingCountAsync(UserId);
        var completed = await _library.GetCompletedCountAsync(UserId);
        return Ok(new { ongoing, completed });
    }

    // ── POST Areas/Mobile/api/Library/chapter-progress ────────────────────
    [HttpPost("chapter-progress")]
    public async Task<IActionResult> LogChapterProgress([FromBody] ChapterProgressRequest req)
    {
        await _library.LogChapterProgressAsync(UserId, req.BookId, req.ChapterNumber);
        return Ok();
    }

    // ── POST Areas/Mobile/api/Library/complete/{bookId} ───────────────────
    [HttpPost("complete/{bookId:guid}")]
    public async Task<IActionResult> MarkComplete(Guid bookId)
    {
        await _library.MarkBookCompleteAsync(UserId, bookId);
        return Ok(new { message = "Book marked as complete." });
    }

    // ── POST Areas/Mobile/api/Library/scroll ──────────────────────────────
    [HttpPost("scroll")]
    public async Task<IActionResult> SaveScroll([FromBody] SaveScrollRequest req)
    {
        await _library.SaveScrollAsync(UserId, req.BookId, req.ScrollPosition);
        return Ok();
    }

    // ── POST Areas/Mobile/api/Library/rate ───────────────────────────────
    [HttpPost("rate")]
    public async Task<IActionResult> Rate([FromBody] RateRequest req)
    {
        var result = await _library.RateBookAsync(UserId, req.BookId, req.Stars);
        return Ok(result);
    }

    // ── GET Areas/Mobile/api/Library/reviews/{bookId} ────────────────────
    [HttpGet("reviews/{bookId:guid}")]
    public async Task<IActionResult> GetReviews(Guid bookId)
    {
        var result = await _library.GetReviewsAsync(UserId, bookId);
        return Ok(result);
    }

    // ── POST Areas/Mobile/api/Library/reviews ─────────────────────────────
    [HttpPost("reviews")]
    public async Task<IActionResult> AddReview([FromBody] AddReviewRequest req)
    {
        var result = await _library.AddReviewAsync(UserId, req.BookId, req.Stars, req.Text);
        return Ok(result);
    }

    // ── POST Areas/Mobile/api/Library/reviews/{reviewId}/like ───────────────
    [HttpPost("reviews/{reviewId:guid}/like")]
    public async Task<IActionResult> ToggleReviewLike(Guid reviewId)
    {
        var (liked, count) = await _library.ToggleLikeAsync(UserId, reviewId);
        return Ok(new { liked, count });
    }

    // ── POST Areas/Mobile/api/Library/reviews/{reviewId}/reply ──────────────
    [HttpPost("reviews/{reviewId:guid}/reply")]
    public async Task<IActionResult> ReplyToReview(Guid reviewId, [FromBody] ReplyRequest req)
    {
        var (success, error) = await _library.ReplyToReviewAsync(UserId, reviewId, req.Text);
        return Ok(new { success, error });
    }

    // ── DELETE Areas/Mobile/api/Library/reviews/{reviewId}/reply ────────────
    [HttpDelete("reviews/{reviewId:guid}/reply")]
    public async Task<IActionResult> DeleteReply(Guid reviewId)
    {
        var (success, error) = await _library.DeleteReplyAsync(UserId, reviewId);
        return Ok(new { success, error });
    }

    // ── POST Areas/Mobile/api/Library/comments ──────────────────────────────
    [HttpPost("comments")]
    public async Task<IActionResult> AddComment([FromBody] AddCommentRequest req)
    {
        var (success, error, comment) = await _library.AddCommentAsync(UserId, req.ReviewId, req.Text);
        return Ok(new { success, error, comment });
    }

    // ── POST Areas/Mobile/api/Library/comments/{commentId}/like ─────────────
    [HttpPost("comments/{commentId:guid}/like")]
    public async Task<IActionResult> ToggleCommentLike(Guid commentId)
    {
        var (liked, count) = await _library.ToggleCommentLikeAsync(UserId, commentId);
        return Ok(new { liked, count });
    }

    // ── POST Areas/Mobile/api/Library/comments/{commentId}/reply ────────────
    [HttpPost("comments/{commentId:guid}/reply")]
    public async Task<IActionResult> AddCommentReply(Guid commentId, [FromBody] CommentReplyRequest req)
    {
        var (success, error, reply) = await _library.AddCommentReplyAsync(
            UserId, req.ReviewId, commentId, req.Text, req.ReplyingTo, req.ParentReplyId);
        return Ok(new { success, error, reply });
    }
}

// ── Request DTOs (inline — move to /DTOs/Mobile/ if preferred) ────────────
public record ChapterProgressRequest(Guid BookId, int ChapterNumber);
public record SaveScrollRequest(Guid BookId, int ScrollPosition);
public record RateRequest(Guid BookId, int Stars);
public record AddReviewRequest(Guid BookId, int Stars, string? Text);
public record ReplyRequest(string Text);
public record AddCommentRequest(Guid ReviewId, string Text);
public record CommentReplyRequest(Guid ReviewId, string Text, string ReplyingTo, Guid? ParentReplyId);