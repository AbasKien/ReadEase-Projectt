using Microsoft.EntityFrameworkCore;
using ReadEase.Data;
using ReadEase.Enums;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;

namespace ReadEase.Repository;

public class BookRepository : IBookRepository
{
    private readonly AppDbContext _db;

    public BookRepository(AppDbContext db) => _db = db;

    // ── Only Active, non-deleted books visible to public ─────────────────
    public async Task<List<Book>> GetAllActiveAsync()
        => await _db.Books
                    .Where(b => !b.IsDeleted && b.Status == BookStatus.Active)
                    .OrderByDescending(b => b.PublishedAt).ToListAsync();

    public async Task<List<Book>> GetFreeAsync()
        => await _db.Books
                    .Where(b => !b.IsDeleted && b.IsFree && b.Status == BookStatus.Active)
                    .OrderByDescending(b => b.PublishedAt).ToListAsync();

    public async Task<List<Book>> SearchAsync(string? title, string? category)
    {
        var q = _db.Books
                   .Where(b => !b.IsDeleted && b.Status == BookStatus.Active)
                   .AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
            q = q.Where(b => b.Title.Contains(title) || b.Author.Contains(title));
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            q = q.Where(b => b.Category.Contains(category));

        return await q.OrderByDescending(b => b.PublishedAt).ToListAsync();
    }

    // ── Author sees ALL their books (active, pending, rejected) ──────────
    public async Task<List<Book>> GetByAuthorUserIdAsync(Guid userId)
        => await _db.Books
                    .Where(b => b.PublishedByUserId == userId)
                    .OrderByDescending(b => b.PublishedAt).ToListAsync();

    // ── Active books only (used by library) ──────────────────────────────
    public async Task<Book?> GetByIdAsync(Guid id)
        => await _db.Books.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

    // ── Any status — used by admin and author ownership checks ───────────
    public async Task<Book?> GetAnyByIdAsync(Guid id)
        => await _db.Books.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Book?> GetDeletedBookByIdAsync(Guid id)
        => await _db.Books.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<List<Book>> GetDeletedAsync()
        => await _db.Books.Where(b => b.IsDeleted)
                          .OrderByDescending(b => b.PublishedAt).ToListAsync();

    // ── Pending premium books waiting for admin approval ─────────────────
    public async Task<List<Book>> GetPendingAsync()
        => await _db.Books
                    .Where(b => !b.IsDeleted && b.Status == BookStatus.PendingApproval)
                    .OrderByDescending(b => b.PublishedAt).ToListAsync();

    public async Task<List<Book>> GetDeletedByAuthorAsync(Guid userId)
        => await _db.Books
                    .Where(b => b.PublishedByUserId == userId && b.IsDeleted)
                    .OrderByDescending(b => b.PublishedAt).ToListAsync();

    public async Task<List<string>> GetCategoriesAsync()
        => await _db.Books
                    .Where(b => !b.IsDeleted && b.Status == BookStatus.Active)
                    .Select(b => b.Category).Distinct().ToListAsync();

    public async Task CreateAsync(Book book)
    {
        await _db.Books.AddAsync(book);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Book book)
    {
        _db.Books.Update(book);
        await _db.SaveChangesAsync();
    }

    public async Task HardDeleteAsync(Guid id)
    {
        var book = await _db.Books.FindAsync(id);
        if (book is not null)
        {
            _db.Books.Remove(book);
            await _db.SaveChangesAsync();
        }
    }

    public async Task UpdateAuthorNameAsync(Guid authorUserId, string newName)
    {
        var books = await _db.Books
            .Where(b => b.PublishedByUserId == authorUserId).ToListAsync();
        foreach (var book in books)
            book.Author = newName;
        if (books.Count > 0)
            await _db.SaveChangesAsync();
    }
}