    namespace ReadEase.ViewModels.Landing;

    public class GuestBookPreviewViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public bool IsFree { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public string PreviewContent { get; set; } = string.Empty;
    }