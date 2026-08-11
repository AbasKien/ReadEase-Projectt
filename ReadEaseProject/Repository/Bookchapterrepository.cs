using ReadEase.Data;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;
using Microsoft.EntityFrameworkCore; 

namespace ReadEase.Repository;

public class BookChapterRepository : IBookChapterRepository
{
    private readonly AppDbContext _db;
    public BookChapterRepository(AppDbContext db) => _db = db;

    public async Task<List<BookChapter>> GetByBookAsync(Guid bookId)
        => await _db.BookChapters
            .Where(c => c.BookId == bookId && !c.IsDeleted)
            .OrderBy(c => c.ChapterNumber)
            .ToListAsync();

    public async Task<BookChapter?> GetByIdAsync(Guid id)
        => await _db.BookChapters.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

    public async Task<int> NextChapterNumberAsync(Guid bookId)
    {
        var max = await _db.BookChapters
            .Where(c => c.BookId == bookId)
            .MaxAsync(c => (int?)c.ChapterNumber) ?? 0;
        return max + 1;
    }

    public async Task<BookChapter> CreateAsync(BookChapter chapter)
    {
        await _db.BookChapters.AddAsync(chapter);
        await _db.SaveChangesAsync();
        return chapter;
    }

    public async Task UpdateAsync(BookChapter chapter)
    {
        _db.BookChapters.Update(chapter);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var c = await _db.BookChapters.FindAsync(id);
        if (c is null) return;
        c.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<Dictionary<Guid, int>> GetChapterCountsByBookIdsAsync(IEnumerable<Guid> bookIds)
    {
        var ids = bookIds.ToList();
        return await _db.BookChapters
            .Where(c => ids.Contains(c.BookId) && !c.IsDeleted)
            .GroupBy(c => c.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BookId, x => x.Count);
    }
}
