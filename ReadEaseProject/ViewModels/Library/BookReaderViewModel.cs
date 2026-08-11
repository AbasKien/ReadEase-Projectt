using ReadEase.ViewModels.Library;

namespace ReadEase.ViewModels.Library
{
    public class BookReaderViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }
        public bool IsFree { get; set; }
        public DateTime PublishedAt { get; set; }
        public int ScrollPosition { get; set; }
        public bool IsFavorite { get; set; }
        public bool CanUseAiRead { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public int UserRating { get; set; }
        public bool IsCompleted { get; set; }

        // ── Chapter support (empty list = legacy single-content book) ─────────
        public List<ChapterNavItem> Chapters { get; set; } = new();
        public int CurrentChapter { get; set; } = 0;
        public bool HasChapters => Chapters.Count > 0;
    }
}