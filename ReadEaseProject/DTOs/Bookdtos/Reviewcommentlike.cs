namespace ReadEase.Models;

public class ReviewCommentLike
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CommentId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ReviewComment Comment { get; set; } = null!;
}