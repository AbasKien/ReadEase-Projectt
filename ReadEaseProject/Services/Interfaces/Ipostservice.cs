using ReadEase.ViewModels.Author;

namespace ReadEase.Services.Interfaces;

public interface IPostService
{
    // ── Author Profile ────────────────────────────────────────────────────
    Task<AuthorProfileViewModel?> GetAuthorProfileAsync(Guid authorUserId, Guid currentUserId);

    // ── Follow ────────────────────────────────────────────────────────────
    Task<bool> ToggleFollowAsync(Guid followerId, Guid followedId);   // returns IsNowFollowing
    Task<bool> IsFollowingAsync(Guid followerId, Guid followedId);

    // ── Posts (Conversations) ─────────────────────────────────────────────
    // wallOwnerId = whose profile wall the post appears on
    // authorId    = who is writing the post (may differ from wallOwnerId)
    Task<PostItemViewModel> CreatePostAsync(Guid authorId, string content, Guid? wallOwnerId = null);
    Task DeletePostAsync(Guid postId, Guid requestingUserId);

    // ── Comments ──────────────────────────────────────────────────────────
    Task<CommentItemViewModel> AddCommentAsync(Guid postId, Guid authorId, string content,
        Guid? parentCommentId = null, string? replyToName = null);
    Task DeleteCommentAsync(Guid commentId, Guid requestingUserId);

    // ── Likes ─────────────────────────────────────────────────────────────
    Task<(int LikeCount, bool IsLiked)> ToggleLikeAsync(Guid postId, Guid userId);

    // ── My profile posts (for Profile.cshtml Conversations tab) ──────────
    Task<List<PostItemViewModel>> GetMyPostsAsync(Guid userId);
}