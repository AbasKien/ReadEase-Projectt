namespace ReadEase.ViewModels.Library.Partials
{
    public class DetailsPanelViewModel
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Category { get; set; } = "";
        public bool IsFree { get; set; }
        public int ChapterCount { get; set; }   // ✅ replaced PublishedAt
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
    }
}