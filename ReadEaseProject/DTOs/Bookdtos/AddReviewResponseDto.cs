using ReadEase.DTOs.Book;

namespace ReadEase.DTOs.Bookdtos;

public class AddReviewResponseDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public double Average { get; set; }
    public int Count { get; set; }
    public List<ReviewDto> Reviews { get; set; } = new();
}