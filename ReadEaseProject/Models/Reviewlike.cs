namespace ReadEase.Models;

public class ReviewLike
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // nav
    public BookReview Review { get; set; } = null!;
    public User User { get; set; } = null!;
}