namespace ReadEase.Models;

public class PostComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // ── Threaded replies ──────────────────────────────────────────────────
    public Guid? ParentCommentId { get; set; }          // null = top-level comment
    public string? ReplyToName { get; set; }            // display name being replied to

    // Navigation
    public UserPost? Post { get; set; }
    public User? Author { get; set; }
    public PostComment? ParentComment { get; set; }
    public ICollection<PostComment> Replies { get; set; } = new List<PostComment>();
}