using System.ComponentModel.DataAnnotations;

namespace ReadEase.ViewModels.Author;

public class ManageChaptersViewModel
{
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public bool IsStoryComplete { get; set; }
    public List<ChapterListItem> Chapters { get; set; } = new();
}
public class ChapterListItem
{
    public Guid Id { get; set; }
    public int ChapterNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public DateTime? ScheduledAt { get; set; }   // ← new
    public bool IsVisible { get; set; } = true; // ← new
}

public class PublishChapterViewModel
{
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int ChapterNumber { get; set; }

    [Required(ErrorMessage = "Chapter title is required.")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Null = publish immediately.
    /// Set = Hangfire will fire at this local datetime (converted to UTC in the controller).
    /// </summary>
    public DateTime? ScheduledAt { get; set; }
}

public class EditChapterViewModel
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int ChapterNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}