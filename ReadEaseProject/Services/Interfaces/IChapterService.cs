using ReadEase.DTOs.Bookdtos;
using ReadEase.ViewModels.Author;

namespace ReadEase.Services.Interfaces;

public interface IChapterService
{
    /// <summary>All chapters for a book (author managing them).</summary>
    Task<ManageChaptersDto> GetManageViewAsync(Guid bookId, Guid authorUserId);

    /// <summary>Pre-filled data for the Add Chapter form.</summary>
    Task<PublishChapterViewModel> GetAddFormAsync(Guid bookId, Guid authorUserId);

    /// <summary>Pre-filled data for the Edit Chapter form.</summary>
    Task<EditChapterViewModel?> GetEditFormAsync(Guid chapterId, Guid authorUserId);

    /// <summary>
    /// Publishes a new chapter (or saves it as scheduled/hidden).
    /// Returns (chapterNumber, chapterId).
    /// </summary>
    Task<(int ChapterNumber, Guid ChapterId)> PublishChapterAsync(
        PublishChapterDto dto, Guid authorUserId, string authorName);

    /// <summary>
    /// Called by Hangfire at ScheduledAt (the FREE unlock time).
    /// Flips IsVisible = true and sets PublishedAt = UtcNow.
    /// </summary>
    Task PublishScheduledChapterAsync(Guid chapterId);

    /// <summary>Persists the Hangfire JobId so it can be cancelled later.</summary>
    Task SaveJobIdAsync(Guid chapterId, string hangfireJobId);

    /// <summary>Saves edits to an existing chapter.</summary>
    Task EditChapterAsync(EditChapterDto dto, Guid authorUserId);

    /// <summary>Soft-deletes a chapter.</summary>
    Task DeleteChapterAsync(Guid chapterId, Guid authorUserId);

    /// <summary>
    /// Resolves chapter data for the reader and detail views.
    /// Pass isPremium / isPremiumPlus / bypassGate to control which chapters
    /// appear in the list. Returns null when the book has no chapters.
    /// </summary>
    Task<ChapterReaderDto?> GetReaderDataAsync(
        Guid bookId, int? requestedChapter,
        bool isPremium = false, bool isPremiumPlus = false, bool bypassGate = false);
}