namespace ReadEase.Models;

/// <summary>
/// A single chapter belonging to a Book.
/// </summary>
public class BookChapter
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public int ChapterNumber { get; set; }          // 1-based display order
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this chapter is publicly visible to readers.
    /// False while scheduled/draft; set to true on publish.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// UTC time at which this chapter is scheduled to become visible to FREE readers.
    /// Null means publish immediately (no delay for anyone).
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// UTC time at which Premium (non-Plus) readers can access this chapter.
    /// Always = ScheduledAt - 24 hours (i.e. 1 day earlier than free unlock).
    /// Null when chapter is published immediately (no delay for premium either).
    /// </summary>
    public DateTime? PremiumUnlocksAt { get; set; }

    /// <summary>
    /// Hangfire background job ID used to flip IsVisible at ScheduledAt.
    /// Null when no scheduled job is pending.
    /// </summary>
    public string? HangfireJobId { get; set; }
}