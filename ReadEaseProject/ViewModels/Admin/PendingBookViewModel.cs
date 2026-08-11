namespace ReadEase.ViewModels.Admin;

public class PendingBookViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public string? CoverImage { get; set; }
    public string? Description { get; set; }
    public DateTime SubmittedAt { get; set; }
    public Guid PublishedByUserId { get; set; }
    public string PublishedByEmail { get; set; } = "";
}