namespace ReadEase.ViewModels.Admin;

// ── Admin: All Books list ─────────────────────────────────────────────────
public class BookManagementViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public bool IsFree { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime PublishedAt { get; set; }
    public int ReadCount { get; set; }
}