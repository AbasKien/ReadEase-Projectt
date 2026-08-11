using ReadEase.Models;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Author;

namespace ReadEase.Services;

public class PostService : IPostService
{
    private readonly IPostRepository _postRepo;
    private readonly IUserRepository _userRepo;
    private readonly IBookRepository _bookRepo;
    private readonly IBookReadRepository _readRepo;

    public PostService(
        IPostRepository postRepo,
        IUserRepository userRepo,
        IBookRepository bookRepo,
        IBookReadRepository readRepo)
    {
        _postRepo = postRepo;
        _userRepo = userRepo;
        _bookRepo = bookRepo;
        _readRepo = readRepo;
    }

    // ────────────────────────────────────────────────────────────────────
    //  AUTHOR PROFILE
    // ────────────────────────────────────────────────────────────────────
    public async Task<AuthorProfileViewModel?> GetAuthorProfileAsync(
        Guid authorUserId, Guid currentUserId)
    {
        var author = await _userRepo.GetByIdAsync(authorUserId);
        if (author is null) return null;

        var books = await _bookRepo.GetByAuthorUserIdAsync(authorUserId);
        var activeBooks = books.Where(b => !b.IsDeleted).ToList();
        var bookIds = activeBooks.Select(b => b.Id).ToList();
        var readCounts = await _readRepo.GetReadCountsBatchAsync(bookIds);

        var followersCount = await _postRepo.GetFollowersCountAsync(authorUserId);
        var followingCount = await _postRepo.GetFollowingCountAsync(authorUserId);
        var isFollowing = currentUserId != Guid.Empty
                          && await _postRepo.IsFollowingAsync(currentUserId, authorUserId);

        var posts = await _postRepo.GetByWallOwnerAsync(authorUserId);

        var followingUsers = await _postRepo.GetFollowingUsersAsync(authorUserId);
        var followingBookCounts = new Dictionary<Guid, int>();
        foreach (var uid in followingUsers.Select(u => u.Id))
        {
            var ub = await _bookRepo.GetByAuthorUserIdAsync(uid);
            followingBookCounts[uid] = ub.Count(b => !b.IsDeleted);
        }

        return new AuthorProfileViewModel
        {
            AuthorUserId = author.Id,
            AuthorName = author.Name,
            ProfilePhoto = author.ProfilePhoto,
            CoverPhoto = author.CoverPhoto,
            TotalBooks = activeBooks.Count,
            TotalReads = readCounts.Values.Sum(),
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            CurrentUserId = currentUserId,
            IsOwnProfile = authorUserId == currentUserId,
            IsFollowing = isFollowing,

            Books = activeBooks.Select(b => new AuthorBookCardViewModel
            {
                Id = b.Id,
                Title = b.Title,
                CoverImage = b.CoverImage,
                Category = b.Category,
                IsFree = b.IsFree,
                ReadCount = readCounts.GetValueOrDefault(b.Id, 0)
            }).ToList(),

            Posts = posts.Select(p => MapPost(p, currentUserId)).ToList(),

            Following = followingUsers.Select(u => new FollowingItemViewModel
            {
                UserId = u.Id,
                Name = u.Name,
                ProfilePhoto = u.ProfilePhoto,
                Role = u.Role.ToString(),
                BookCount = followingBookCounts.GetValueOrDefault(u.Id, 0)
            }).ToList()
        };
    }

    // ────────────────────────────────────────────────────────────────────
    //  FOLLOW
    // ────────────────────────────────────────────────────────────────────
    public async Task<bool> ToggleFollowAsync(Guid followerId, Guid followedId)
    {
        if (followerId == followedId) return false;

        var existing = await _postRepo.GetFollowAsync(followerId, followedId);
        if (existing is not null)
        {
            await _postRepo.RemoveFollowAsync(existing);
            return false;
        }

        await _postRepo.AddFollowAsync(new UserFollow
        {
            FollowerId = followerId,
            FollowedId = followedId
        });
        return true;
    }

    public async Task<bool> IsFollowingAsync(Guid followerId, Guid followedId)
        => await _postRepo.IsFollowingAsync(followerId, followedId);

    // ────────────────────────────────────────────────────────────────────
    //  POSTS
    // ────────────────────────────────────────────────────────────────────
    public async Task<PostItemViewModel> CreatePostAsync(
        Guid authorId, string content, Guid? wallOwnerId = null)
    {
        var post = new UserPost
        {
            AuthorId = authorId,
            WallOwnerId = wallOwnerId ?? authorId,
            Content = content.Trim()
        };
        await _postRepo.CreateAsync(post);

        var author = await _userRepo.GetByIdAsync(authorId);
        return new PostItemViewModel
        {
            PostId = post.Id,
            AuthorUserId = authorId,
            AuthorName = author?.Name ?? "Unknown",
            AuthorPhoto = author?.ProfilePhoto,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            LikeCount = 0,
            IsLikedByCurrentUser = false,
            IsOwnPost = true,
            Comments = new()
        };
    }

    public async Task DeletePostAsync(Guid postId, Guid requestingUserId)
    {
        var post = await _postRepo.GetByIdAsync(postId)
            ?? throw new InvalidOperationException("Post not found.");

        // ✅ Allow: post author OR the wall owner can delete
        if (post.AuthorId != requestingUserId && post.WallOwnerId != requestingUserId)
            throw new UnauthorizedAccessException("Not authorized to delete this post.");

        await _postRepo.SoftDeleteAsync(postId);
    }

    // ────────────────────────────────────────────────────────────────────
    //  COMMENTS
    // ────────────────────────────────────────────────────────────────────
    public async Task<CommentItemViewModel> AddCommentAsync(
        Guid postId, Guid authorId, string content,
        Guid? parentCommentId = null, string? replyToName = null)
    {
        var comment = new PostComment
        {
            PostId = postId,
            AuthorId = authorId,
            Content = content.Trim(),
            ParentCommentId = parentCommentId,
            ReplyToName = replyToName
        };
        await _postRepo.AddCommentAsync(comment);

        var author = await _userRepo.GetByIdAsync(authorId);
        return new CommentItemViewModel
        {
            CommentId = comment.Id,
            AuthorUserId = authorId,
            AuthorName = author?.Name ?? "Unknown",
            AuthorPhoto = author?.ProfilePhoto,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            IsOwnComment = true,
            ParentCommentId = parentCommentId,
            ReplyToName = replyToName,
            Replies = new()
        };
    }

    public async Task DeleteCommentAsync(Guid commentId, Guid requestingUserId)
    {
        var comment = await _postRepo.GetCommentByIdAsync(commentId)
            ?? throw new InvalidOperationException("Comment not found.");

        // ✅ Allow: comment author OR the wall owner of the post can delete
        var post = await _postRepo.GetByIdAsync(comment.PostId);
        bool isCommentAuthor = comment.AuthorId == requestingUserId;
        bool isWallOwner = post is not null && post.WallOwnerId == requestingUserId;

        if (!isCommentAuthor && !isWallOwner)
            throw new UnauthorizedAccessException("Not authorized to delete this comment.");

        await _postRepo.SoftDeleteCommentAsync(commentId);
    }

    // ────────────────────────────────────────────────────────────────────
    //  LIKES
    // ────────────────────────────────────────────────────────────────────
    public async Task<(int LikeCount, bool IsLiked)> ToggleLikeAsync(Guid postId, Guid userId)
    {
        var existing = await _postRepo.GetLikeAsync(postId, userId);
        if (existing is not null)
        {
            await _postRepo.RemoveLikeAsync(existing);
            var count = await _postRepo.GetLikeCountAsync(postId);
            return (count, false);
        }

        await _postRepo.AddLikeAsync(new PostLike { PostId = postId, UserId = userId });
        var newCount = await _postRepo.GetLikeCountAsync(postId);
        return (newCount, true);
    }

    // ────────────────────────────────────────────────────────────────────
    //  MY POSTS
    // ────────────────────────────────────────────────────────────────────
    public async Task<List<PostItemViewModel>> GetMyPostsAsync(Guid userId)
    {
        var posts = await _postRepo.GetByWallOwnerAsync(userId);
        return posts.Select(p => MapPost(p, userId)).ToList();
    }

    // ── Private helpers ───────────────────────────────────────────────────
    private static PostItemViewModel MapPost(UserPost p, Guid currentUserId)
        => new()
        {
            PostId = p.Id,
            AuthorUserId = p.AuthorId,
            AuthorName = p.Author?.Name ?? "Unknown",
            AuthorPhoto = p.Author?.ProfilePhoto,
            Content = p.Content,
            CreatedAt = p.CreatedAt,
            LikeCount = p.Likes.Count,
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
            // ✅ IsOwnPost: true if you wrote it OR it's on your wall
            IsOwnPost = p.AuthorId == currentUserId || p.WallOwnerId == currentUserId,
            Comments = p.Comments
                .Where(c => c.ParentCommentId == null && !c.IsDeleted)
                .Select(c => MapComment(c, currentUserId))
                .ToList()
        };

    private static CommentItemViewModel MapComment(PostComment c, Guid currentUserId)
        => new()
        {
            CommentId = c.Id,
            AuthorUserId = c.AuthorId,
            AuthorName = c.Author?.Name ?? "Unknown",
            AuthorPhoto = c.Author?.ProfilePhoto,
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            IsOwnComment = c.AuthorId == currentUserId,
            ParentCommentId = c.ParentCommentId,
            ReplyToName = c.ReplyToName,
            Replies = c.Replies
                .Where(r => !r.IsDeleted)
                .Select(r => MapComment(r, currentUserId))
                .ToList()
        };
}