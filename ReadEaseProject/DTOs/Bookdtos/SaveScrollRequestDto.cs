namespace ReadEase.DTOs.Book
{
    public class SaveScrollRequestDto
    {
        public Guid BookId { get; set; }
        public int ScrollPosition { get; set; }
    }
}
