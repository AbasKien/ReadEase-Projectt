namespace ReadEase.DTOs.Book
{
    public class ReplyToReviewRequestDto
    {
        public Guid ReviewId { get; set; }
        public string ReplyText { get; set; } = "";
    }
}