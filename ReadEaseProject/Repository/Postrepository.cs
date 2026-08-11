using Microsoft.EntityFrameworkCore;
using ReadEase.Data;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;

namespace ReadEase.Repository;

public class PostRepository : IPostRepository
{
    private readonly AppDbContext _db;

    public PostRepository(AppDbContext db)
    {
        _db = db;
    }

    // ────────────────────────────────────────────────────────────────────
    //  POSTS
    // ────────────────────────────────────────────────────────────────────

    public async Task<UserPost> CreateAsync(UserPost post)
    {
        _db.UserPosts.Add(post);
        await _db.SaveChangesAsync();
        return post;
    }

    public async Task<UserPost?> GetByIdAsync(Guid postId)
        => await _db.UserPosts
            .Include(p => p.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Replies)
                    .ThenInclude(r => r.Author)
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

    public async Task<List<UserPost>> GetByWallOwnerAsync(Guid wallOwnerId)
        => await _db.UserPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Replies)
                    .ThenInclude(r => r.Author)
            .Include(p => p.Likes)
            .Where(p => p.WallOwnerId == wallOwnerId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<List<UserPost>> GetByAuthorAsync(Guid authorUserId)
        => await _db.UserPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Replies)
                    .ThenInclude(r => r.Author)
            .Include(p => p.Likes)
            .Where(p => p.AuthorId == authorUserId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<List<PostComment>> GetTopLevelCommentsAsync(Guid postId)
        => await _db.PostComments
            .AsNoTracking()
            .Include(c => c.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .Where(c => c.PostId == postId && c.ParentCommentId == null && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

    public async Task SoftDeleteAsync(Guid postId)
    {
        var post = await _db.UserPosts.FindAsync(postId);
        if (post is null) return;
        post.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ────────────────────────────────────────────────────────────────────
    //  COMMENTS
    // ────────────────────────────────────────────────────────────────────

    public async Task<PostComment> AddCommentAsync(PostComment comment)
    {
        _db.PostComments.Add(comment);
        await _db.SaveChangesAsync();
        return comment;
    }

    public async Task<PostComment?> GetCommentByIdAsync(Guid commentId)
        => await _db.PostComments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

    public async Task SoftDeleteCommentAsync(Guid commentId)
    {
        var comment = await _db.PostComments.FindAsync(commentId);
        if (comment is null) return;
        comment.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ────────────────────────────────────────────────────────────────────
    //  LIKES
    // ────────────────────────────────────────────────────────────────────

    public async Task<PostLike?> GetLikeAsync(Guid postId, Guid userId)
        => await _db.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

    public async Task AddLikeAsync(PostLike like)
    {
        _db.PostLikes.Add(like);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveLikeAsync(PostLike like)
    {
        _db.PostLikes.Remove(like);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetLikeCountAsync(Guid postId)
        => await _db.PostLikes.CountAsync(l => l.PostId == postId);

    // ────────────────────────────────────────────────────────────────────
    //  FOLLOWS
    // ────────────────────────────────────────────────────────────────────

    public async Task<UserFollow?> GetFollowAsync(Guid followerId, Guid followedId)
        => await _db.UserFollows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);

    public async Task AddFollowAsync(UserFollow follow)
    {
        _db.UserFollows.Add(follow);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveFollowAsync(UserFollow follow)
    {
        _db.UserFollows.Remove(follow);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetFollowersCountAsync(Guid userId)
        => await _db.UserFollows.CountAsync(f => f.FollowedId == userId);

    public async Task<int> GetFollowingCountAsync(Guid userId)
        => await _db.UserFollows.CountAsync(f => f.FollowerId == userId);

    public async Task<bool> IsFollowingAsync(Guid followerId, Guid followedId)
        => await _db.UserFollows
            .AnyAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);

    public async Task<List<User>> GetFollowingUsersAsync(Guid userId)
        => await _db.UserFollows
            .AsNoTracking()
            .Where(f => f.FollowerId == userId)
            .Include(f => f.Followed)
            .Select(f => f.Followed!)
            .ToListAsync();
}