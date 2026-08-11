namespace ReadEase.Models;

public class BookRating
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public int Stars { get; set; }  // 1–5
    public DateTime RatedAt { get; set; } = DateTime.UtcNow;
}