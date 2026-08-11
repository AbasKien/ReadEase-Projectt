using ReadEase.Enums;

namespace ReadEase.ViewModels.Author;

public class ManageBookItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public bool IsFree { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime PublishedAt { get; set; }
    public int ReadCount { get; set; }

    // ✅ Approval status
    public BookStatus Status { get; set; } = BookStatus.Active;
    public string? RejectionReason { get; set; }

    public bool IsStoryComplete { get; set; } = false; // ✅ NEW

    public int ChapterNumber { get; set; }


}