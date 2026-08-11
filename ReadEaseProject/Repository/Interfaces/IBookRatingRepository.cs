using ReadEase.DTOs.Bookdtos;
using ReadEase.Models;

namespace ReadEase.Repository.Interfaces;

public interface IBookRatingRepository
{
    Task<BookRating?> GetUserRatingAsync(Guid userId, Guid bookId);
    Task<double> GetAverageRatingAsync(Guid bookId);
    Task<int> GetRatingCountAsync(Guid bookId);

    /// <summary>
    /// Top N books sorted by average star rating descending.
    /// Only books with at least minVotes ratings are included.
    /// </summary>
    Task<List<TopPickItem>> GetTopRatedAsync(int take = 5, int minVotes = 1);

    Task UpsertAsync(Guid userId, Guid bookId, int stars);
}