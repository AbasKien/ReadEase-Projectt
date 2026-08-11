namespace ReadEase.DTOs.Bookdtos
{
    public class TopPickItem
    {
        public Guid BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public double AvgRating { get; set; }
        public int RatingCount { get; set; }
        public bool IsFree { get; set; }
    }
}
