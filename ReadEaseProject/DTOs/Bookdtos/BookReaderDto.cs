namespace ReadEase.DTOs.Bookdtos;

public class BookReaderDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImage { get; set; }
    public bool IsFree { get; set; }
    public DateTime PublishedAt { get; set; }
    public int ScrollPosition { get; set; }
    public bool IsFavorite { get; set; }
}