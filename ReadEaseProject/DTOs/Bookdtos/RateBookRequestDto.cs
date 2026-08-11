namespace ReadEase.DTOs.Book
{
    public class RateBookRequestDto
    {
        public Guid BookId { get; set; }
        public int Stars { get; set; }   // 1–5
    }
}
