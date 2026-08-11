// Add this DTO class in the same file or a separate DTOs file
public class SimilarBookDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public string? CoverImage { get; set; }
    public bool IsFree { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
}