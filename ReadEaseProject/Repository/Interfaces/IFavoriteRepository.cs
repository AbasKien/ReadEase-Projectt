using ReadEase.Models;

namespace ReadEase.Repository.Interfaces
{
    public interface IFavoriteRepository
    {
        Task<List<Favorite>> GetByUserAsync(Guid userId);
        Task<bool> IsFavoriteAsync(Guid userId, Guid bookId);
        Task<bool> ToggleAsync(Guid userId, Guid bookId);

        /// <summary>
        /// Returns distinct user IDs who favorited any book by this author.
        /// Excludes the author themselves. Used by ChapterService for fan-out notifications.
        /// </summary>
        Task<List<Guid>> GetFanUserIdsForAuthorAsync(Guid authorUserId);

        /// <summary>
        /// Returns the full User objects for all users who have favorited a specific book.
        /// Excludes the book's own author. Used by SubscriptionService for tier-aware fan-out.
        /// </summary>
        Task<List<User>> GetFanUsersForBookAsync(Guid bookId);
    }
}