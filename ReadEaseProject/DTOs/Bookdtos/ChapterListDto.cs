namespace ReadEase.DTOs.Bookdtos;

public class ChapterListDto
{
    public Guid Id { get; set; }
    public int ChapterNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// UTC time this chapter becomes visible to FREE readers (= ScheduledAt).
    /// Null when published immediately.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// UTC time this chapter unlocks for PREMIUM (non-Plus) readers.
    /// = ScheduledAt - 24 hours. Null when published immediately.
    /// </summary>
    public DateTime? PremiumUnlocksAt { get; set; }

    public bool IsVisible { get; set; }
}