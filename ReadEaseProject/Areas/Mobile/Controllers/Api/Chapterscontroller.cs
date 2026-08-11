using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.DTOs.Bookdtos;
using ReadEase.Services.Interfaces;
using System.Security.Claims;

namespace ReadEase.Areas.Mobile.Controllers.Api;

[Area("Mobile")]
[Route("Areas/Mobile/api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class ChaptersController : ControllerBase
{
    private readonly IChapterService _chapters;
    private readonly ILibraryService _library;

    public ChaptersController(IChapterService chapters, ILibraryService library)
    {
        _chapters = chapters;
        _library = library;
    }

    private Guid UserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string UserName =>
        User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

    // ── GET Areas/Mobile/api/Chapters/{bookId} ────────────────────────────
    // Returns chapter list for a book (reader view — chapter number + title)
    [HttpGet("{bookId:guid}")]
    public async Task<IActionResult> GetChapters(Guid bookId, [FromQuery] int? chapter)
    {
        var isPremiumPlus = await _library.IsPremiumPlusAsync(UserId);
        var isPremium = isPremiumPlus || await _library.IsSubscribedAsync(UserId);
        var bypassGate = User.IsInRole("Admin") || User.IsInRole("Author");

        var data = await _chapters.GetReaderDataAsync(
            bookId, chapter, isPremium, isPremiumPlus, bypassGate);

        if (data is null)
            return NotFound(new { message = "No chapters found for this book." });

        return Ok(data);
    }

    // ── GET Areas/Mobile/api/Chapters/manage/{bookId} ─────────────────────
    // Author: get all chapters for a book they own
    [HttpGet("manage/{bookId:guid}")]
    public async Task<IActionResult> GetManage(Guid bookId)
    {
        try
        {
            var data = await _chapters.GetManageViewAsync(bookId, UserId);
            return Ok(data);
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

    // ── GET Areas/Mobile/api/Chapters/single/{chapterId} ──────────────────
    // Author: get a single chapter with full content (used by mobile edit form)
    // Reuses GetEditFormAsync — the same method the web EditChapter page uses
    [HttpGet("single/{chapterId:guid}")]
    public async Task<IActionResult> GetSingle(Guid chapterId)
    {
        try
        {
            var vm = await _chapters.GetEditFormAsync(chapterId, UserId);
            if (vm is null)
                return NotFound(new { message = "Chapter not found." });

            // Return only the fields the mobile edit form needs
            return Ok(new
            {
                id = vm.Id,
                title = vm.Title,
                content = vm.Content
            });
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

    // ── POST Areas/Mobile/api/Chapters/publish ────────────────────────────
    // Author: publish a new chapter
    [HttpPost("publish")]
    public async Task<IActionResult> PublishChapter([FromBody] PublishChapterDto dto)
    {
        try
        {
            var chapterNum = await _chapters.PublishChapterAsync(dto, UserId, UserName);
            return Ok(new { chapterNumber = chapterNum, message = "Chapter published successfully." });
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

    // ── PUT Areas/Mobile/api/Chapters/edit ────────────────────────────────
    // Author: edit an existing chapter
    [HttpPut("edit")]
    public async Task<IActionResult> EditChapter([FromBody] EditChapterDto dto)
    {
        try
        {
            await _chapters.EditChapterAsync(dto, UserId);
            return Ok(new { message = "Chapter updated." });
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

    // ── DELETE Areas/Mobile/api/Chapters/{chapterId} ──────────────────────
    // Author: soft-delete a chapter
    [HttpDelete("{chapterId:guid}")]
    public async Task<IActionResult> DeleteChapter(Guid chapterId)
    {
        try
        {
            await _chapters.DeleteChapterAsync(chapterId, UserId);
            return Ok(new { message = "Chapter deleted." });
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
}