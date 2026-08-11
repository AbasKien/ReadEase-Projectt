using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.Services.Interfaces;
using System.Security.Claims;

namespace ReadEase.Controllers;

[Authorize]
public class PostsController : Controller
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    // ── GET /Posts/GetMyPosts ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetMyPosts()
    {
        var posts = await _postService.GetMyPostsAsync(GetUserId());
        return Json(posts);
    }

    // ── GET /Posts/AuthorProfile ──────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> AuthorProfile(Guid authorUserId)
    {
        var vm = await _postService.GetAuthorProfileAsync(authorUserId, GetUserId());
        if (vm is null) return NotFound();
        return View("~/Views/Author/AuthorProfile.cshtml", vm);
    }

    // ── POST /Posts/ToggleFollow ──────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ToggleFollow([FromBody] ToggleFollowRequest req)
    {
        var isNowFollowing = await _postService.ToggleFollowAsync(GetUserId(), req.FollowedId);
        return Json(new { isFollowing = isNowFollowing });
    }

    // ── POST /Posts/CreatePost ────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content) || req.Content.Length > 1000)
            return BadRequest(new { error = "Content must be 1–1000 characters." });

        var post = await _postService.CreatePostAsync(GetUserId(), req.Content, req.WallOwnerId);
        return Json(post);
    }

    // ── POST /Posts/DeletePost ────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> DeletePost([FromBody] PostIdRequest req)
    {
        try
        {
            await _postService.DeletePostAsync(req.PostId, GetUserId());
            return Ok();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── POST /Posts/AddComment ────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] AddCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content) || req.Content.Length > 500)
            return BadRequest(new { error = "Comment must be 1–500 characters." });

        var comment = await _postService.AddCommentAsync(
            req.PostId, GetUserId(), req.Content,
            req.ParentCommentId, req.ReplyToName);
        return Json(comment);
    }

    // ── POST /Posts/DeleteComment ─────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> DeleteComment([FromBody] CommentIdRequest req)
    {
        try
        {
            await _postService.DeleteCommentAsync(req.CommentId, GetUserId());
            return Ok();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── POST /Posts/ToggleLike ────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ToggleLike([FromBody] PostIdRequest req)
    {
        var (likeCount, isLiked) = await _postService.ToggleLikeAsync(req.PostId, GetUserId());
        return Json(new { likeCount, isLiked });
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────
public record ToggleFollowRequest(Guid FollowedId);
public record CreatePostRequest(string Content, Guid? WallOwnerId = null);
public record PostIdRequest(Guid PostId);
public record AddCommentRequest(Guid PostId, string Content,
    Guid? ParentCommentId = null, string? ReplyToName = null);
public record CommentIdRequest(Guid CommentId);