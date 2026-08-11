namespace ReadEase.DTOs.Bookdtos;

public class BookDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string? Description { get; set; }
    public bool IsFree { get; set; }
    public DateTime PublishedAt { get; set; }
    public bool IsFavorite { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int UserRating { get; set; }
    public bool CanRead { get; set; }
}