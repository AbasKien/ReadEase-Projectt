namespace ReadEase.DTOs.Landing;

public class LandingBookDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public bool IsFree { get; set; }
    public double AverageRating { get; set; }
    public int ReadCount { get; set; }
    public string PreviewContent { get; set; } = string.Empty;
}