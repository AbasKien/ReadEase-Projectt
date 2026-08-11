using ReadEase.Enums;

namespace ReadEase.Models;

public class Book
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsFree { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public Guid PublishedByUserId { get; set; }
    public BookStatus Status { get; set; } = BookStatus.Active;
    public string? RejectionReason { get; set; }

    // ✅ NEW — author marks story as fully published/completed
    public bool IsStoryComplete { get; set; } = false;
}