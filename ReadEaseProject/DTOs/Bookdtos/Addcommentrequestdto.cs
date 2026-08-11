namespace ReadEase.DTOs.Bookdtos;

public class AddCommentRequestDto
{
    public Guid ReviewId { get; set; }
    public string Text { get; set; } = "";
}