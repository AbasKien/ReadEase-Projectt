namespace ReadEase.DTOs.Bookdtos;

public class AddReviewRequestDto
{
    public Guid BookId { get; set; }
    public int Stars { get; set; }
    public string? Text { get; set; }
}