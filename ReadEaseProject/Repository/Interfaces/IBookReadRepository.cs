using ReadEase.Models;

namespace ReadEase.Repository.Interfaces;

public interface IBookReadRepository
{
    Task LogAsync(Guid userId, Guid bookId);
    Task LogChapterAsync(Guid userId, Guid bookId, int chapterNumber);
    Task<int> GetTotalReadsByBookAsync(Guid bookId);
    Task<int> GetTotalReadsByUserAsync(Guid userId);
    Task<Dictionary<Guid, int>> GetReadCountsBatchAsync(IEnumerable<Guid> bookIds);
    Task<List<BookRead>> GetRecentByUserAsync(Guid userId, int take = 5);
    Task MarkCompleteAsync(Guid userId, Guid bookId);
    Task<List<BookRead>> GetOngoingByUserAsync(Guid userId);
    Task<List<BookRead>> GetCompletedByUserAsync(Guid userId);
    Task<(int Ongoing, int Completed)> GetStatusCountsAsync(Guid userId);
    Task<BookRead?> GetAsync(Guid userId, Guid bookId);

    /// <summary>
    /// Called when author marks story complete.
    /// Auto-completes all readers who have already read every chapter.
    /// </summary>
    Task AutoCompleteFinishedReadersAsync(Guid bookId);
}