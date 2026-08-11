namespace ReadEase.DTOs.Bookdtos;

public class PublishChapterDto
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    /// <summary>UTC scheduled release time. Null = publish immediately.</summary>
    public DateTime? ScheduledAt { get; set; }
}