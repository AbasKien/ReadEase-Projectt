using Microsoft.EntityFrameworkCore;
using ReadEase.Data;
using ReadEase.DTOs.Bookdtos;
using ReadEase.Enums;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;

namespace ReadEase.Repository;

public class BookRatingRepository : IBookRatingRepository
{
    private readonly AppDbContext _db;
    public BookRatingRepository(AppDbContext db) => _db = db;

    public Task<BookRating?> GetUserRatingAsync(Guid userId, Guid bookId)
        => _db.BookRatings.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);

    public async Task<double> GetAverageRatingAsync(Guid bookId)
    {
        var stars = await _db.BookRatings
                             .Where(r => r.BookId == bookId)
                             .Select(r => r.Stars)
                             .ToListAsync();
        return stars.Count == 0 ? 0 : Math.Round(stars.Average(), 1);
    }

    public Task<int> GetRatingCountAsync(Guid bookId)
        => _db.BookRatings.CountAsync(r => r.BookId == bookId);

    public async Task UpsertAsync(Guid userId, Guid bookId, int stars)
    {
        stars = Math.Clamp(stars, 1, 5);
        var existing = await GetUserRatingAsync(userId, bookId);
        if (existing is null)
            _db.BookRatings.Add(new BookRating
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BookId = bookId,
                Stars = stars
            });
        else
        {
            existing.Stars = stars;
            existing.RatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Returns top N books sorted by average star rating descending.
    /// Only Active, non-deleted books with at least minVotes ratings are included.
    /// </summary>
    public async Task<List<TopPickItem>> GetTopRatedAsync(int take = 5, int minVotes = 1)
    {
        return await _db.BookRatings
            .Include(r => r.Book)
            .Where(r => !r.Book.IsDeleted
                     && r.Book.Status == BookStatus.Active) // ✅ FIX: exclude Pending/Rejected
            .GroupBy(r => r.BookId)
            .Where(g => g.Count() >= minVotes)
            .Select(g => new TopPickItem
            {
                BookId = g.Key,
                Title = g.First().Book.Title,
                Author = g.First().Book.Author,
                CoverImage = g.First().Book.CoverImage,
                AvgRating = Math.Round(g.Average(r => (double)r.Stars), 1),
                RatingCount = g.Count(),
                IsFree = g.First().Book.IsFree
            })
            .OrderByDescending(x => x.AvgRating)
            .ThenByDescending(x => x.RatingCount)
            .Take(take)
            .ToListAsync();
    }
}