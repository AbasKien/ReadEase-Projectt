using ReadEase.DTOs.Book;
using ReadEase.DTOs.Bookdtos;
using ReadEase.Models;

namespace ReadEase.ViewModels.Library;

public class BookDetailViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string? Description { get; set; }
    public bool IsFree { get; set; }
    public DateTime PublishedAt { get; set; }
    public int ChapterCount { get; set; }
    public bool IsStoryComplete { get; set; }
    public List<ChapterNavItem> Chapters { get; set; } = [];
    public List<SimilarBookDto> SimilarBooks { get; set; } = [];
    public bool IsFavorite { get; set; }
    public bool CanRead { get; set; }
    /// <summary>Downloads and AI Voice Reader are PremiumPlus only.</summary>
    public bool CanDownload { get; set; }
    public bool CanUseAiRead { get; set; }
    public bool IsSubscribed { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsBookAuthor { get; set; }
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string? AuthorProfilePhoto { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int UserRating { get; set; }
    public List<ReviewDto> Reviews { get; set; } = [];

    // ── Reading progress for current user ─────────────────────────────────
    public int UserLastChapterRead { get; set; } = 0;
    public int UserProgressPercent { get; set; } = 0;

    // ── Author profile card ───────────────────────────────────────────────
    public Guid AuthorUserId { get; set; }          // ✅ NEW
    public bool IsFollowingAuthor { get; set; }     // ✅ NEW
}