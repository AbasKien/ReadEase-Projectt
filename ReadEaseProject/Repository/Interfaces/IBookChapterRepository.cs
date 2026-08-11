using ReadEase.Models;

namespace ReadEase.Repository.Interfaces;

public interface IBookChapterRepository
{
    /// <summary>All non-deleted chapters for a book, ordered by ChapterNumber.</summary>
    Task<List<BookChapter>> GetByBookAsync(Guid bookId);

    /// <summary>Single chapter by its own Id.</summary>
    Task<BookChapter?> GetByIdAsync(Guid id);

    /// <summary>Next chapter number for a book (max + 1, or 1 if none).</summary>
    Task<int> NextChapterNumberAsync(Guid bookId);

    Task<BookChapter> CreateAsync(BookChapter chapter);
    Task UpdateAsync(BookChapter chapter);
    Task DeleteAsync(Guid id);   // soft-delete

    /// <summary>Returns chapter count per bookId in a single query.</summary>
    Task<Dictionary<Guid, int>> GetChapterCountsByBookIdsAsync(IEnumerable<Guid> bookIds);
}

