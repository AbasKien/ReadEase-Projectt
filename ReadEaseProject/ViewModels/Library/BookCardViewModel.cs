namespace ReadEase.ViewModels.Library
{
    public class BookCardViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public string? Description { get; set; }
        public bool IsFree { get; set; }
        public bool IsFavorite { get; set; }
        public int ReadCount { get; set; }
        public DateTime PublishedAt { get; set; }   // ← added from upload
        public bool IsStoryComplete { get; set; }
    }
}
