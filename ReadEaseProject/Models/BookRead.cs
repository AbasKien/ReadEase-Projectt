using ReadEase.Enums;

namespace ReadEase.Models;

public class BookRead
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }

    // ✅ NEW — tracks furthest chapter the user has read
    public int LastChapterRead { get; set; } = 0;
}