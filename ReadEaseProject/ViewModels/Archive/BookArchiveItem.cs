namespace ReadEase.ViewModels.Archive
{
    public class BookArchiveItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
    }
}
