using ReadEase.Data;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ReadEase.Repository
{
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly AppDbContext _db;
        public FavoriteRepository(AppDbContext db) => _db = db;

        public async Task<List<Favorite>> GetByUserAsync(Guid userId)
            => await _db.Favorites
                .Include(f => f.Book)
                .Where(f => f.UserId == userId && !f.Book.IsDeleted)
                .OrderByDescending(f => f.SavedAt).ToListAsync();

        public async Task<bool> IsFavoriteAsync(Guid userId, Guid bookId)
            => await _db.Favorites.AnyAsync(f => f.UserId == userId && f.BookId == bookId);

        public async Task<bool> ToggleAsync(Guid userId, Guid bookId)
        {
            var fav = await _db.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == bookId);
            if (fav is not null)
            {
                _db.Favorites.Remove(fav);
                await _db.SaveChangesAsync();
                return false;
            }
            await _db.Favorites.AddAsync(new Favorite
            {
                UserId = userId,
                BookId = bookId,
                SavedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Returns distinct user IDs who have favorited any book by this author.
        /// Excludes the author themselves.
        /// Used by ChapterService to fan-out new chapter notifications.
        /// </summary>
        public async Task<List<Guid>> GetFanUserIdsForAuthorAsync(Guid authorUserId)
            => await _db.Favorites
                .Where(f => f.Book.PublishedByUserId == authorUserId
                         && f.UserId != authorUserId
                         && !f.Book.IsDeleted)
                .Select(f => f.UserId)
                .Distinct()
                .ToListAsync();

        /// <summary>
        /// Returns the full User objects for all users who have favorited a specific book.
        /// Excludes the book's own author.
        /// Used by SubscriptionService for tier-aware fan-out notifications.
        /// </summary>
        public async Task<List<User>> GetFanUsersForBookAsync(Guid bookId)
            => await _db.Favorites
                .Include(f => f.User)
                .Where(f => f.BookId == bookId
                         && f.UserId != f.Book.PublishedByUserId
                         && !f.Book.IsDeleted)
                .Select(f => f.User)
                .Distinct()
                .ToListAsync();
    }
}