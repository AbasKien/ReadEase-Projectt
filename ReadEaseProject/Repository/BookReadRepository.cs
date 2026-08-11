using Microsoft.EntityFrameworkCore;
using ReadEase.Data;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;

namespace ReadEase.Repository;

public class BookReadRepository : IBookReadRepository
{
    private readonly AppDbContext _db;
    public BookReadRepository(AppDbContext db) => _db = db;

    public async Task LogAsync(Guid userId, Guid bookId)
    {
        var existing = await _db.BookReads
            .FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);

        if (existing is not null)
        {
            existing.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return;
        }

        await _db.BookReads.AddAsync(new BookRead
        {
            UserId = userId,
            BookId = bookId,
            ReadAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Tracks the furthest chapter the user has read.
    /// Auto-marks IsCompleted = true ONLY when:
    ///   - The book's author has marked IsStoryComplete = true, AND
    ///   - The user has read every chapter (LastChapterRead >= totalChapters)
    /// This means a book stays Ongoing even at 100% until the author marks it complete.
    /// </summary>
    public async Task LogChapterAsync(Guid userId, Guid bookId, int chapterNumber)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == bookId);
        var totalChapters = await _db.BookChapters.CountAsync(c => c.BookId == bookId);

        var existing = await _db.BookReads
            .FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);

        if (existing is not null)
        {
            existing.ReadAt = DateTime.UtcNow;

            if (chapterNumber > existing.LastChapterRead)
                existing.LastChapterRead = chapterNumber;

            // Auto-complete ONLY when author has marked story complete
            // AND user has now finished all chapters
            if (book is not null
                && book.IsStoryComplete
                && totalChapters > 0
                && existing.LastChapterRead >= totalChapters
                && !existing.IsCompleted)
            {
                existing.IsCompleted = true;
                existing.CompletedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return;
        }

        // First time reading this book
        var newRecord = new BookRead
        {
            UserId = userId,
            BookId = bookId,
            ReadAt = DateTime.UtcNow,
            LastChapterRead = chapterNumber
        };

        // Edge case: single-chapter completed story read for the first time
        if (book is not null
            && book.IsStoryComplete
            && totalChapters > 0
            && chapterNumber >= totalChapters)
        {
            newRecord.IsCompleted = true;
            newRecord.CompletedAt = DateTime.UtcNow;
        }

        await _db.BookReads.AddAsync(newRecord);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Called the moment the author marks a story as complete.
    /// Finds all readers who have already finished every chapter and
    /// flips their IsCompleted flag so they move to the Completed list.
    /// </summary>
    public async Task AutoCompleteFinishedReadersAsync(Guid bookId)
    {
        var totalChapters = await _db.BookChapters.CountAsync(c => c.BookId == bookId);
        if (totalChapters == 0) return;

        var readers = await _db.BookReads
            .Where(r => r.BookId == bookId
                     && !r.IsCompleted
                     && r.LastChapterRead >= totalChapters)
            .ToListAsync();

        foreach (var r in readers)
        {
            r.IsCompleted = true;
            r.CompletedAt = DateTime.UtcNow;
        }

        if (readers.Count > 0)
            await _db.SaveChangesAsync();
    }

    public async Task<int> GetTotalReadsByBookAsync(Guid bookId)
        => await _db.BookReads.CountAsync(r => r.BookId == bookId);

    public async Task<Dictionary<Guid, int>> GetReadCountsBatchAsync(IEnumerable<Guid> bookIds)
    {
        var ids = bookIds.ToList();
        return await _db.BookReads
            .Where(r => ids.Contains(r.BookId))
            .GroupBy(r => r.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BookId, x => x.Count);
    }

    public async Task<int> GetTotalReadsByUserAsync(Guid userId)
        => await _db.BookReads
            .Where(r => r.UserId == userId)
            .Select(r => r.BookId)
            .Distinct()
            .CountAsync();

    public async Task<List<BookRead>> GetRecentByUserAsync(Guid userId, int take = 5)
        => await _db.BookReads
            .Include(r => r.Book)
            .Where(r => r.UserId == userId && !r.Book.IsDeleted)
            .OrderByDescending(r => r.ReadAt)
            .Take(take).ToListAsync();

    public async Task MarkCompleteAsync(Guid userId, Guid bookId)
    {
        var allRecords = await _db.BookReads
            .Where(r => r.UserId == userId && r.BookId == bookId)
            .OrderByDescending(r => r.ReadAt)
            .ToListAsync();

        if (allRecords.Count == 0)
        {
            await _db.BookReads.AddAsync(new BookRead
            {
                UserId = userId,
                BookId = bookId,
                ReadAt = DateTime.UtcNow,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            });
        }
        else
        {
            if (allRecords.Count > 1)
                _db.BookReads.RemoveRange(allRecords.Skip(1));

            var record = allRecords.First();
            record.IsCompleted = true;
            record.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<BookRead>> GetOngoingByUserAsync(Guid userId)
    {
        var all = await _db.BookReads
            .Include(r => r.Book)
            .Where(r => r.UserId == userId && !r.IsCompleted && !r.Book.IsDeleted)
            .OrderByDescending(r => r.ReadAt)
            .ToListAsync();

        return all.GroupBy(r => r.BookId).Select(g => g.First()).ToList();
    }

    public async Task<List<BookRead>> GetCompletedByUserAsync(Guid userId)
    {
        var all = await _db.BookReads
            .Include(r => r.Book)
            .Where(r => r.UserId == userId && r.IsCompleted && !r.Book.IsDeleted)
            .OrderByDescending(r => r.CompletedAt)
            .ToListAsync();

        return all.GroupBy(r => r.BookId).Select(g => g.First()).ToList();
    }

    public async Task<(int Ongoing, int Completed)> GetStatusCountsAsync(Guid userId)
    {
        var bookIds = await _db.Books
            .Where(b => !b.IsDeleted)
            .Select(b => b.Id)
            .ToListAsync();

        var records = await _db.BookReads
            .Where(r => r.UserId == userId && bookIds.Contains(r.BookId))
            .ToListAsync();

        var deduped = records
            .GroupBy(r => r.BookId)
            .Select(g => g.OrderByDescending(r => r.ReadAt).First())
            .ToList();

        return (deduped.Count(r => !r.IsCompleted), deduped.Count(r => r.IsCompleted));
    }

    public async Task<BookRead?> GetAsync(Guid userId, Guid bookId)
        => await _db.BookReads
            .Where(r => r.UserId == userId && r.BookId == bookId)
            .OrderByDescending(r => r.ReadAt)
            .FirstOrDefaultAsync();
}