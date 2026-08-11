namespace ReadEase.ViewModels.Library
{
    public class RecentBookItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
    }
}