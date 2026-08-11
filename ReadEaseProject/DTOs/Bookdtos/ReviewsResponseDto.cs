namespace ReadEase.DTOs.Book
{
    public class ReviewsResponseDto
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public double Average { get; set; }
        public int Count { get; set; }
        public int MyStars { get; set; }
        public List<ReviewDto> Reviews { get; set; } = new();
    }
}