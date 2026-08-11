using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReadEase.Services.Interfaces;
using System.Security.Claims;

namespace ReadEase.Areas.Mobile.Controllers.Api;

[Authorize(AuthenticationSchemes = "Bearer")]
[Route("Areas/Mobile/api/[controller]")]
[ApiController]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IAuthorService _author;

    public PostsController(IPostService postService, IAuthorService author)
    {
        _postService = postService;
        _author = author;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    // ── GET Areas/Mobile/api/Posts/author-profile/{authorUserId} ───────────
    [HttpGet("author-profile/{authorUserId:guid}")]
    public async Task<IActionResult> GetAuthorProfile(Guid authorUserId)
    {
        var vm = await _postService.GetAuthorProfileAsync(authorUserId, UserId);
        if (vm is null) return NotFound(new { message = "Author not found." });

        return Ok(new
        {
            authorUserId = vm.AuthorUserId,
            authorName = vm.AuthorName,
            profilePhoto = vm.ProfilePhoto,
            coverPhoto = vm.CoverPhoto,
            totalBooks = vm.TotalBooks,
            totalReads = vm.TotalReads,
            followersCount = vm.FollowersCount,
            followingCount = vm.FollowingCount,
            isOwnProfile = vm.IsOwnProfile,
            isFollowing = vm.IsFollowing,
            posts = vm.Posts.Select(p => new
            {
                postId = p.PostId,
                authorName = p.AuthorName,
                authorPhoto = p.AuthorPhoto,
                authorUserId = p.AuthorUserId,
                content = p.Content,
                createdAt = p.CreatedAt,
                likeCount = p.LikeCount,
                isLikedByCurrentUser = p.IsLikedByCurrentUser,
                isOwnPost = p.IsOwnPost,
                comments = p.Comments.Select(c => new
                {
                    commentId = c.CommentId,
                    authorUserId = c.AuthorUserId,
                    authorName = c.AuthorName,
                    authorPhoto = c.AuthorPhoto,
                    content = c.Content,
                    createdAt = c.CreatedAt,
                    isOwnComment = c.IsOwnComment,
                    parentCommentId = c.ParentCommentId,
                    replyToName = c.ReplyToName,
                    replies = c.Replies.Select(r => new
                    {
                        commentId = r.CommentId,
                        authorUserId = r.AuthorUserId,
                        authorName = r.AuthorName,
                        authorPhoto = r.AuthorPhoto,
                        content = r.Content,
                        createdAt = r.CreatedAt,
                        isOwnComment = r.IsOwnComment,
                        parentCommentId = r.ParentCommentId,
                        replyToName = r.ReplyToName
                    }).ToList()
                }).ToList()
            }).ToList(),
            following = vm.Following.Select(f => new
            {
                userId = f.UserId,
                name = f.Name,
                profilePhoto = f.ProfilePhoto,
                role = f.Role,
                bookCount = f.BookCount
            }).ToList(),
            books = vm.Books.Select(b => new
            {
                id = b.Id,
                title = b.Title,
                coverImage = b.CoverImage,
                category = b.Category,
                isFree = b.IsFree,
                readCount = b.ReadCount
            }).ToList()
        });
    }

    // ── POST Areas/Mobile/api/Posts/toggle-follow ──────────────────────────
    [HttpPost("toggle-follow")]
    public async Task<IActionResult> ToggleFollow([FromBody] ToggleFollowRequest req)
    {
        var isNowFollowing = await _postService.ToggleFollowAsync(UserId, req.FollowedId);
        return Ok(new { isFollowing = isNowFollowing });
    }

    // ── GET Areas/Mobile/api/Posts/my-posts ────────────────────────────────
    [HttpGet("my-posts")]
    public async Task<IActionResult> GetMyPosts()
    {
        var posts = await _postService.GetMyPostsAsync(UserId);
        return Ok(posts);
    }

    // ── POST Areas/Mobile/api/Posts/create ─────────────────────────────────
    [HttpPost("create")]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content) || req.Content.Length > 1000)
            return BadRequest(new { error = "Content must be 1–1000 characters." });

        var post = await _postService.CreatePostAsync(UserId, req.Content, req.WallOwnerId);
        return Ok(post);
    }

    // ── DELETE Areas/Mobile/api/Posts/{postId} ─────────────────────────────
    [HttpDelete("{postId:guid}")]
    public async Task<IActionResult> DeletePost(Guid postId)
    {
        try
        {
            await _postService.DeletePostAsync(postId, UserId);
            return Ok(new { success = true });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── POST Areas/Mobile/api/Posts/{postId}/comment ─────────────────────
    [HttpPost("{postId:guid}/comment")]
    public async Task<IActionResult> AddComment(Guid postId, [FromBody] AddPostCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content) || req.Content.Length > 500)
            return BadRequest(new { error = "Comment must be 1–500 characters." });

        var comment = await _postService.AddCommentAsync(
            postId, UserId, req.Content, req.ParentCommentId, req.ReplyToName);
        return Ok(comment);
    }

    // ── DELETE Areas/Mobile/api/Posts/comments/{commentId} ────────────────
    [HttpDelete("comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        try
        {
            await _postService.DeleteCommentAsync(commentId, UserId);
            return Ok(new { success = true });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // ── POST Areas/Mobile/api/Posts/{postId}/like ──────────────────────────
    [HttpPost("{postId:guid}/like")]
    public async Task<IActionResult> ToggleLike(Guid postId)
    {
        var (likeCount, isLiked) = await _postService.ToggleLikeAsync(postId, UserId);
        return Ok(new { likeCount, isLiked });
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────
public record ToggleFollowRequest(Guid FollowedId);
public record CreatePostRequest(string Content, Guid? WallOwnerId = null);
public record AddPostCommentRequest(string Content, Guid? ParentCommentId = null, string? ReplyToName = null);
