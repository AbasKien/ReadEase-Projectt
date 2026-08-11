using ReadEase.DTOs.Auth;
using ReadEase.DTOs.Landing;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Landing;

namespace ReadEase.Services;

public class LandingService : ILandingService
{
    // Characters shown as the free teaser on the landing page cards
    private const int PreviewLength = 300;

    // Characters shown to unauthenticated users on the guest read page
    private const int GuestPreviewLength = 500;

    private readonly IBookRepository _bookRepo;
    private readonly IBookRatingRepository _ratingRepo;
    private readonly IBookReadRepository _readRepo;
    private readonly IBookChapterRepository _chapterRepo;

    public LandingService(
        IBookRepository bookRepo,
        IBookRatingRepository ratingRepo,
        IBookReadRepository readRepo,
        IBookChapterRepository chapterRepo)
    {
        _bookRepo = bookRepo;
        _ratingRepo = ratingRepo;
        _readRepo = readRepo;
        _chapterRepo = chapterRepo;
    }

    public async Task<LandingViewModel> GetLandingDataAsync()
    {
        var allBooks = await _bookRepo.GetAllActiveAsync();
        var allIds = allBooks.Select(b => b.Id).ToList();
        var categoryMap = allBooks.ToDictionary(b => b.Id, b => b.Category);

        // ── Top Rated ──────────────────────────────────────────────────
        var topPicks = await _ratingRepo.GetTopRatedAsync(take: 5, minVotes: 1);
        var topIds = topPicks.Select(t => t.BookId).ToList();
        var topReads = await _readRepo.GetReadCountsBatchAsync(topIds);

        var topRated = topPicks.Select(t =>
        {
            var book = allBooks.FirstOrDefault(b => b.Id == t.BookId);
            return new LandingBookDto
            {
                Id = t.BookId,
                Title = t.Title,
                Author = t.Author,
                Category = categoryMap.GetValueOrDefault(t.BookId, string.Empty),
                CoverImage = t.CoverImage,
                IsFree = t.IsFree,
                AverageRating = t.AvgRating,
                ReadCount = topReads.GetValueOrDefault(t.BookId, 0),
                PreviewContent = Truncate(book?.Content)
            };
        }).ToList();

        // ── Most Viewed ────────────────────────────────────────────────
        var allReadCounts = await _readRepo.GetReadCountsBatchAsync(allIds);
        var ratingMap = topPicks.ToDictionary(t => t.BookId, t => t.AvgRating);

        var mostViewed = allBooks
            .Select(b => new LandingBookDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category,
                CoverImage = b.CoverImage,
                IsFree = b.IsFree,
                ReadCount = allReadCounts.GetValueOrDefault(b.Id, 0),
                AverageRating = ratingMap.GetValueOrDefault(b.Id, 0),
                PreviewContent = Truncate(b.Content)
            })
            .OrderByDescending(b => b.ReadCount)
            .Take(8)
            .ToList();

        // ── Genres ────────────────────────────────────────────────────
        var genres = allBooks
            .Where(b => !string.IsNullOrWhiteSpace(b.Category))
            .SelectMany(b => b.Category
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .Select(g => new LandingGenreDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(12)
            .ToList();

        // ── All Books (for genre panel) ────────────────────────────────
        // Re-uses allReadCounts already fetched above — no extra DB call
        var allBookDtos = allBooks
            .Select(b => new LandingBookDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category,
                CoverImage = b.CoverImage,
                IsFree = b.IsFree,
                ReadCount = allReadCounts.GetValueOrDefault(b.Id, 0),
                AverageRating = ratingMap.GetValueOrDefault(b.Id, 0),
                PreviewContent = string.Empty
            })
            .ToList();

        return new LandingViewModel
        {
            TopRated = topRated,
            MostViewed = mostViewed,
            AllBooks = allBookDtos,   // ← populated now
            Genres = genres,
            TotalBooks = allBooks.Count,
            TotalGenres = allBooks
                .Where(b => !string.IsNullOrWhiteSpace(b.Category))
                .SelectMany(b => b.Category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count()
        };
    }

    // ── Guest book preview — pulls Chapter 1 content ─────────────────────
    public async Task<GuestBookPreviewViewModel?> GetBookPreviewAsync(Guid bookId)
    {
        var book = await _bookRepo.GetByIdAsync(bookId);
        if (book is null) return null;

        var avgRating = await _ratingRepo.GetAverageRatingAsync(bookId);
        var ratingCount = await _ratingRepo.GetRatingCountAsync(bookId);

        var chapters = await _chapterRepo.GetByBookAsync(bookId);
        var chapter1 = chapters
            .OrderBy(c => c.ChapterNumber)
            .FirstOrDefault();

        var previewContent = GuestTruncate(chapter1?.Content ?? book.Content);

        return new GuestBookPreviewViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Category = book.Category,
            CoverImage = book.CoverImage,
            IsFree = book.IsFree,
            AverageRating = avgRating,
            RatingCount = ratingCount,
            PreviewContent = previewContent
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private static string Truncate(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;
        content = content.Trim();
        if (content.Length <= PreviewLength) return content;
        var cut = content.LastIndexOf(' ', PreviewLength);
        return cut > 0 ? content[..cut] : content[..PreviewLength];
    }

    private static string GuestTruncate(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;
        content = content.Trim();
        if (content.Length <= GuestPreviewLength) return content;
        var cut = content.LastIndexOf(' ', GuestPreviewLength);
        return cut > 0 ? content[..cut] : content[..GuestPreviewLength];
    }
}