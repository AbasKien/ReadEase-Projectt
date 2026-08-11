using ReadEase.DTOs.Book;
using ReadEase.DTOs.Bookdtos;
using ReadEase.Models;
using ReadEase.ViewModels.Library;

namespace ReadEase.Services.Interfaces;

public interface ILibraryService
{
    Task<(List<Book> Books, List<Guid> FavoriteIds)>
        GetLibraryAsync(Guid userId, bool isSubscribed, string? search, string? category);

    // Raw tuple overload — used internally / by legacy callers
    Task<(Book? Book, int ScrollPosition)>
        OpenBookAsync(Guid userId, Guid bookId, bool isSubscribed);

    // Full ViewModel overload — used by LibraryController
    Task<BookReaderViewModel?> OpenBookAsync(Guid userId, Guid bookId, bool isSubscribed, string role);

    Task<BookDetailViewModel?> GetBookDetailAsync(Guid userId, Guid bookId, bool premium, bool isAdmin, bool isPremiumPlus = false);
    Task<List<SimilarBookDto>> GetSimilarBooksAsync(Guid excludeBookId, string category, int limit = 6);
    Task SaveScrollAsync(Guid userId, Guid bookId, int scrollPosition);
    Task<bool> ToggleFavoriteAsync(Guid userId, Guid bookId);
    Task<List<Favorite>> GetFavoritesAsync(Guid userId);
    Task<List<BookRead>> GetRecentlyReadAsync(Guid userId, int take = 6);
    Task<List<string>> GetCategoriesAsync();
    Task<Book?> GetBookByIdAsync(Guid bookId);
    Task<bool> IsSubscribedAsync(Guid userId);
    Task<bool> IsPremiumPlusAsync(Guid userId);
    Task<Dictionary<Guid, int>> GetReadCountsBatchAsync(IEnumerable<Guid> bookIds);
    Task<List<TopPickItem>> GetTopPicksAsync(int take = 5, int minVotes = 1);
    Task<RatingResponseDto> RateBookAsync(Guid userId, Guid bookId, int stars);
    Task<AddReviewResponseDto> AddReviewAsync(Guid userId, Guid bookId, int stars, string? text);
    Task<ReviewsResponseDto> GetReviewsAsync(Guid userId, Guid bookId);

    // Author reply
    Task<bool> IsBookAuthorAsync(Guid userId, Guid bookId);
    Task<(bool Success, string? Error)> ReplyToReviewAsync(Guid userId, Guid reviewId, string replyText);
    Task<(bool Success, string? Error)> DeleteReplyAsync(Guid userId, Guid reviewId);

    // Likes
    Task<(bool Liked, int Count)> ToggleLikeAsync(Guid userId, Guid reviewId);
    Task<(bool Liked, int Count)> ToggleCommentLikeAsync(Guid userId, Guid commentId);

    // Comments
    Task<(bool Success, string? Error, ReviewCommentDto? Comment)>
        AddCommentAsync(Guid userId, Guid reviewId, string text);
    Task<(bool Success, string? Error, CommentReplyDto? Reply)>
        AddCommentReplyAsync(Guid userId, Guid reviewId, Guid commentId,
                             string text, string? replyingTo, Guid? parentReplyId = null);

    // Reading progress
    Task LogChapterProgressAsync(Guid userId, Guid bookId, int chapterNumber);
    Task MarkBookCompleteAsync(Guid userId, Guid bookId);
    Task<bool> IsBookCompletedAsync(Guid userId, Guid bookId);
    Task<ReadingProgressViewModel> GetOngoingBooksAsync(Guid userId, bool premium);
    Task<ReadingProgressViewModel> GetCompletedBooksAsync(Guid userId, bool premium);
    Task<int> GetOngoingCountAsync(Guid userId);
    Task<int> GetCompletedCountAsync(Guid userId);

    // ── Featured sections ───────────────────────────────────────────────
    Task<List<Book>> GetFeaturedTopPicksAsync(int count = 10);
    Task<List<Book>> GetFeaturedNewReleasesAsync(int count = 10);
    Task<List<Book>> GetFeaturedRecommendedAsync(Guid userId, int count = 10);
}