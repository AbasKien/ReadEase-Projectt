using ReadEase.Data;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ReadEase.Repository
{
    public class BookmarkRepository : IBookmarkRepository
    {
        private readonly AppDbContext _db;
        public BookmarkRepository(AppDbContext db) => _db = db;

        public async Task<Bookmark?> GetAsync(Guid userId, Guid bookId)
            => await _db.Bookmarks
                .FirstOrDefaultAsync(x => x.UserId == userId && x.BookId == bookId);

        public async Task UpsertAsync(Guid userId, Guid bookId, int scrollPosition)
        {
            var bm = await GetAsync(userId, bookId);
            if (bm is null)
            {
                await _db.Bookmarks.AddAsync(new Bookmark
                {
                    UserId = userId,
                    BookId = bookId,
                    ScrollPosition = scrollPosition,
                    SavedAt = DateTime.UtcNow
                });
            }
            else
            {
                bm.ScrollPosition = scrollPosition;
                bm.SavedAt = DateTime.UtcNow;
                _db.Bookmarks.Update(bm);
            }
            await _db.SaveChangesAsync();
        }
    }
}

