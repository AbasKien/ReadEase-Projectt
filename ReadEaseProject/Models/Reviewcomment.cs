namespace ReadEase.Models;

public class ReviewComment
{
    public Guid? ParentCommentId { get; set; }   // was: internal field (not a property — EF ignores it)

    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // nav
    public BookReview Review { get; set; } = null!;
    public User User { get; set; } = null!;

    public string? ReplyingTo { get; set; }      // was: non-nullable string with no default (CS8618 warning + runtime null risk)
}