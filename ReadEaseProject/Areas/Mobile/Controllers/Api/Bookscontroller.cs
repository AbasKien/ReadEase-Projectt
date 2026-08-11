using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.Services.Interfaces;
using System.Security.Claims;

namespace ReadEase.Areas.Mobile.Controllers.Api;

[Area("Mobile")]
[Route("Areas/Mobile/api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class BooksController : ControllerBase
{
    private readonly IAuthorService _author;
    private readonly IWebHostEnvironment _env;

    public BooksController(IAuthorService author, IWebHostEnvironment env)
    {
        _author = author;
        _env    = env;
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string UserName =>
        User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

    private string UserEmail =>
        User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    // ── GET Areas/Mobile/api/Books/dashboard ─────────────────────────────
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var vm = await _author.GetDashboardAsync(UserId, UserName, UserEmail);
        return Ok(vm);
    }

    // ── GET Areas/Mobile/api/Books/mybooks ───────────────────────────────
    [HttpGet("mybooks")]
    public async Task<IActionResult> GetMyBooks()
    {
        var vm = await _author.GetMyBooksAsync(UserId);
        return Ok(vm);
    }

    // ── GET Areas/Mobile/api/Books/archive ───────────────────────────────
    [HttpGet("archive")]
    public async Task<IActionResult> GetArchive()
    {
        var items = await _author.GetArchiveAsync(UserId);
        return Ok(items);
    }

    // ── POST Areas/Mobile/api/Books/archive/{bookId} ─────────────────────
    [HttpPost("archive/{bookId:guid}")]
    public async Task<IActionResult> ArchiveBook(Guid bookId)
    {
        try
        {
            await _author.ArchiveBookAsync(bookId, UserId);
            return Ok(new { message = "Book archived." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST Areas/Mobile/api/Books/restore/{bookId} ─────────────────────
    [HttpPost("restore/{bookId:guid}")]
    public async Task<IActionResult> RestoreBook(Guid bookId)
    {
        try
        {
            await _author.RestoreBookAsync(bookId, UserId);
            return Ok(new { message = "Book restored." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── DELETE Areas/Mobile/api/Books/{bookId} ────────────────────────────
    [HttpDelete("{bookId:guid}")]
    public async Task<IActionResult> PermanentDelete(Guid bookId)
    {
        try
        {
            await _author.PermanentDeleteAsync(bookId, UserId);
            return Ok(new { message = "Book permanently deleted." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST Areas/Mobile/api/Books/toggle-complete/{bookId} ─────────────
    [HttpPost("toggle-complete/{bookId:guid}")]
    public async Task<IActionResult> ToggleComplete(Guid bookId)
    {
        try
        {
            var newState = await _author.ToggleStoryCompleteAsync(bookId, UserId);
            return Ok(new { isStoryComplete = newState });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── POST Areas/Mobile/api/Books/publish ───────────────────────────────
    // Publishes a new book — accepts multipart/form-data for cover image
    [HttpPost("publish")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PublishBook([FromForm] MobilePublishBookRequest req)
    {
        try
        {
            var vm = new ReadEase.ViewModels.Author.PublishBookViewModel
            {
                Title          = req.Title,
                Category       = req.Category,
                Description    = req.Description,
                IsFree         = req.IsFree,
                CoverImageFile = req.CoverImage
            };

            var (isPending, wasPromoted) =
                await _author.PublishBookAsync(UserId, UserName, vm, _env.WebRootPath);

            return Ok(new
            {
                isPending,
                wasPromoted,
                message = isPending
                    ? "Book submitted for admin approval."
                    : "Book published successfully!"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── PUT Areas/Mobile/api/Books/edit ───────────────────────────────────
    [HttpPut("edit")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> EditBook([FromForm] MobileEditBookRequest req)
    {
        try
        {
            var vm = new ReadEase.ViewModels.Author.PublishBookViewModel
            {
                Id             = req.Id,
                Title          = req.Title,
                Category       = req.Category,
                Description    = req.Description,
                IsFree         = req.IsFree,
                CoverImageFile = req.CoverImage
            };

            var isPending = await _author.EditBookAsync(UserId, vm, _env.WebRootPath);
            return Ok(new
            {
                isPending,
                message = isPending
                    ? "Book submitted for premium approval."
                    : "Book updated successfully."
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

// ── Request models (multipart/form-data) ─────────────────────────────────
public class MobilePublishBookRequest
{
    public string Title        { get; set; } = string.Empty;
    public string Category     { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsFree         { get; set; } = true;
    public IFormFile? CoverImage { get; set; }
}

public class MobileEditBookRequest : MobilePublishBookRequest
{
    public Guid Id { get; set; }
}