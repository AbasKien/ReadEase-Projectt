namespace ReadEase.ViewModels.Author;

public class AuthorProfileViewModel
{
    // ── Author info ───────────────────────────────────────────────────────
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? ProfilePhoto { get; set; }
    public string? CoverPhoto { get; set; }

    // ── Stats ─────────────────────────────────────────────────────────────
    public int TotalBooks { get; set; }
    public int TotalReads { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }

    // ── Current viewer state ──────────────────────────────────────────────
    public Guid CurrentUserId { get; set; }
    public bool IsOwnProfile { get; set; }
    public bool IsFollowing { get; set; }

    // ── Tabs ──────────────────────────────────────────────────────────────
    public List<PostItemViewModel> Posts { get; set; } = new();
    public List<FollowingItemViewModel> Following { get; set; } = new();
    public List<AuthorBookCardViewModel> Books { get; set; } = new();
}

public class PostItemViewModel
{
    public Guid PostId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorPhoto { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public bool IsOwnPost { get; set; }
    public List<CommentItemViewModel> Comments { get; set; } = new();
}

public class CommentItemViewModel
{
    public Guid CommentId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorPhoto { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsOwnComment { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string? ReplyToName { get; set; }
    public List<CommentItemViewModel> Replies { get; set; } = new();
}

public class FollowingItemViewModel
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProfilePhoto { get; set; }
    public string Role { get; set; } = string.Empty;
    public int BookCount { get; set; }
}

public class AuthorBookCardViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public int ReadCount { get; set; }
}