using ReadEase.Models;

namespace ReadEase.Repository.Interfaces;

public interface IPostRepository
{
    // ── Posts ──────────────────────────────────────────────────────────────
    Task<UserPost> CreateAsync(UserPost post);
    Task<UserPost?> GetByIdAsync(Guid postId);
    Task<List<UserPost>> GetByWallOwnerAsync(Guid wallOwnerId);   // all posts ON a profile wall
    Task<List<UserPost>> GetByAuthorAsync(Guid authorUserId);     // all posts WRITTEN by a user
    Task SoftDeleteAsync(Guid postId);

    // ── Comments ───────────────────────────────────────────────────────────
    Task<PostComment> AddCommentAsync(PostComment comment);
    Task<PostComment?> GetCommentByIdAsync(Guid commentId);
    Task SoftDeleteCommentAsync(Guid commentId);
    Task<List<PostComment>> GetTopLevelCommentsAsync(Guid postId);   // only root comments + their replies

    // ── Likes ──────────────────────────────────────────────────────────────
    Task<PostLike?> GetLikeAsync(Guid postId, Guid userId);
    Task AddLikeAsync(PostLike like);
    Task RemoveLikeAsync(PostLike like);
    Task<int> GetLikeCountAsync(Guid postId);

    // ── Follows ────────────────────────────────────────────────────────────
    Task<UserFollow?> GetFollowAsync(Guid followerId, Guid followedId);
    Task AddFollowAsync(UserFollow follow);
    Task RemoveFollowAsync(UserFollow follow);
    Task<int> GetFollowersCountAsync(Guid userId);
    Task<int> GetFollowingCountAsync(Guid userId);
    Task<bool> IsFollowingAsync(Guid followerId, Guid followedId);
    Task<List<User>> GetFollowingUsersAsync(Guid userId);
}