namespace ReadEase.DTOs.Bookdtos;

public class BookCardDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string? Description { get; set; }
    public bool IsFree { get; set; }
    public bool IsFavorite { get; set; }
}