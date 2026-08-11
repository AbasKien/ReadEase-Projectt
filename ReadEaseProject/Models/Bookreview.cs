namespace ReadEase.Models;

public class BookReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public int Stars { get; set; }             // 1–5
    public string? Text { get; set; }          // optional written review
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Author reply ──────────────────────────────────────────────────────
    public string? AuthorReply { get; set; }
    public DateTime? AuthorRepliedAt { get; set; }
}