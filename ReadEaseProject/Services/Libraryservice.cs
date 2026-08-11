using ReadEase.DTOs.Book;
using ReadEase.DTOs.Bookdtos;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Library;

namespace ReadEase.Services;

public class LibraryService : ILibraryService
{
    private readonly IBookRepository _bookRepo;
    private readonly IBookReadRepository _readRepo;
    private readonly IBookmarkRepository _bookmarkRepo;
    private readonly IFavoriteRepository _favoriteRepo;
    private readonly IBookRatingRepository _ratingRepo;
    private readonly IBookReviewRepository _reviewRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPostRepository _postRepo;
    private readonly IBookChapterRepository _chapterRepo;

    public LibraryService(
        IBookRepository bookRepo,
        IBookReadRepository readRepo,
        IBookmarkRepository bookmarkRepo,
        IFavoriteRepository favoriteRepo,
        IBookRatingRepository ratingRepo,
        IBookReviewRepository reviewRepo,
        IUserRepository userRepo,
        IPostRepository postRepo,
        IBookChapterRepository chapterRepo)
    {
        _bookRepo = bookRepo;
        _readRepo = readRepo;
        _bookmarkRepo = bookmarkRepo;
        _favoriteRepo = favoriteRepo;
        _ratingRepo = ratingRepo;
        _reviewRepo = reviewRepo;
        _userRepo = userRepo;
        _postRepo = postRepo;
        _chapterRepo = chapterRepo;
    }

    // ── Library browsing ──────────────────────────────────────────────────
    public async Task<(List<Book> Books, List<Guid> FavoriteIds)> GetLibraryAsync(
        Guid userId, bool isSubscribed, string? search, string? category)
    {
        var allBooks = await _bookRepo.SearchAsync(search, category);
        var favorites = await _favoriteRepo.GetByUserAsync(userId);
        var favoriteIds = favorites.Select(f => f.BookId).ToList();
        return (allBooks, favoriteIds);
    }

    // ── Open / read a book (raw tuple — used internally) ──────────────────
    public async Task<(Book? Book, int ScrollPosition)> OpenBookAsync(
        Guid userId, Guid bookId, bool isSubscribed)
    {
        var book = await _bookRepo.GetByIdAsync(bookId);
        if (book is null) return (null, 0);

        if (!book.IsFree && !isSubscribed)
            throw new UnauthorizedAccessException("This book requires a subscription.");

        await _readRepo.LogAsync(userId, bookId);
        var bookmark = await _bookmarkRepo.GetAsync(userId, bookId);
        return (book, bookmark?.ScrollPosition ?? 0);
    }

    // ── Open / read a book (full ViewModel — used by LibraryController) ───
    public async Task<BookReaderViewModel?> OpenBookAsync(
        Guid userId, Guid bookId, bool isSubscribed, string role)
    {
        var (book, scroll) = await OpenBookAsync(userId, bookId, isSubscribed);
        if (book is null) return null;

        var isFav = await _favoriteRepo.IsFavoriteAsync(userId, bookId);
        var avgRating = await _ratingRepo.GetAverageRatingAsync(bookId);
        var ratingCnt = await _ratingRepo.GetRatingCountAsync(bookId);
        var userRating = await _ratingRepo.GetUserRatingAsync(userId, bookId);
        var isPremiumPlus = await IsPremiumPlusAsync(userId);
        var isAdmin = role == nameof(ReadEase.Enums.UserRole.Admin);
        var isAuthor = book.PublishedByUserId == userId;

        // ── Only expose VISIBLE chapters to readers ───────────────────────
        // Authors and Admins see all chapters (including scheduled ones).
        var allChapters = await _chapterRepo.GetByBookAsync(bookId);
        var visibleChapters = (isAdmin || isAuthor)
            ? allChapters
            : allChapters.Where(c => c.IsVisible).ToList();

        var readRecord = await _readRepo.GetAsync(userId, bookId);
        var lastChapterRead = readRecord?.LastChapterRead ?? 0;
        var startChapter = lastChapterRead > 0
            ? lastChapterRead
            : visibleChapters.Any() ? visibleChapters.Min(c => c.ChapterNumber) : 1;

        return new BookReaderViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Category = book.Category,
            Content = book.Content,
            Description = book.Description,
            CoverImage = book.CoverImage,
            IsFree = book.IsFree,
            PublishedAt = book.PublishedAt,
            ScrollPosition = scroll,
            IsFavorite = isFav,
            CanUseAiRead = isAdmin || isPremiumPlus,
            AverageRating = avgRating,
            RatingCount = ratingCnt,
            UserRating = userRating?.Stars ?? 0,
            Chapters = visibleChapters.Select(c => new ChapterNavItem
            {
                Id = c.Id,
                ChapterNumber = c.ChapterNumber,
                Title = c.Title,
                Content = c.Content
            }).ToList(),
            CurrentChapter = startChapter
        };
    }

    // ── Book detail ───────────────────────────────────────────────────────
    public async Task<BookDetailViewModel?> GetBookDetailAsync(
        Guid userId, Guid bookId, bool premium, bool isAdmin, bool isPremiumPlus = false)
    {
        var book = await _bookRepo.GetByIdAsync(bookId);
        if (book is null) return null;

        var isFav = await _favoriteRepo.IsFavoriteAsync(userId, bookId);
        var avgRating = await _ratingRepo.GetAverageRatingAsync(bookId);
        var ratingCnt = await _ratingRepo.GetRatingCountAsync(bookId);
        var userRating = await _ratingRepo.GetUserRatingAsync(userId, bookId);
        var similar = await GetSimilarBooksAsync(bookId, book.Category, 6);
        var isAuthor = book.PublishedByUserId == userId;

        // ── Chapters shown in the detail/chapter list ─────────────────────
        // Admins and the book's author see everything.
        // Everyone else sees: live (IsVisible) + scheduled chapters
        //   — scheduled ones appear LOCKED with release date shown.
        //   The actual read-gate is enforced in the Read action.
        var allChapters = await _chapterRepo.GetByBookAsync(bookId);
        var now = DateTime.UtcNow;
        var listableChapters = (isAdmin || isAuthor)
            ? allChapters
            : allChapters.Where(c =>
                c.IsVisible ||
                c.ScheduledAt.HasValue ||
                (!c.IsVisible && !c.ScheduledAt.HasValue && c.PublishedAt <= now)
            ).ToList();

        // For "live chapter count" we still only count truly visible chapters
        var visibleChapters = allChapters.Where(c => c.IsVisible).ToList();

        // ── Reading progress (based on visible chapters only) ─────────────
        // lastChapterRead stores a chapter NUMBER (e.g. 4), not a count.
        // Divide by the highest visible chapter number, not the count,
        // and clamp to 100 so stale reads on deleted chapters never exceed 100%.
        var readRecord = await _readRepo.GetAsync(userId, bookId);
        var lastChapterRead = readRecord?.LastChapterRead ?? 0;

        var maxChapterNumber = visibleChapters.Count > 0
            ? visibleChapters.Max(c => c.ChapterNumber)
            : 0;

        var clampedLastChapter = maxChapterNumber > 0
            ? Math.Min(lastChapterRead, maxChapterNumber)
            : 0;

        var progressPercent = maxChapterNumber > 0 && clampedLastChapter > 0
            ? Math.Min(100, (int)Math.Round((double)clampedLastChapter / maxChapterNumber * 100))
            : 0;

        // ── Author info ───────────────────────────────────────────────────
        var authorUser = await _userRepo.GetByIdAsync(book.PublishedByUserId);
        var authorDisplayName = authorUser?.Name ?? book.Author;
        var authorPhoto = authorUser?.ProfilePhoto;
        var isFollowing = userId != Guid.Empty
            && await _postRepo.IsFollowingAsync(userId, book.PublishedByUserId);

        return new BookDetailViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            Category = book.Category,
            CoverImage = book.CoverImage,
            Description = book.Description,
            IsFree = book.IsFree,
            PublishedAt = book.PublishedAt,
            IsStoryComplete = book.IsStoryComplete,
            IsFavorite = isFav,
            AverageRating = avgRating,
            RatingCount = ratingCnt,
            UserRating = userRating?.Stars ?? 0,
            CanRead = isAdmin || book.IsFree || premium,
            CanDownload = isAdmin || isPremiumPlus,
            CanUseAiRead = isAdmin || isPremiumPlus,
            IsSubscribed = premium,
            IsAdmin = isAdmin,
            IsBookAuthor = isAuthor,

            // ── ChapterCount = only live (visible) chapters ───────────────
            // Scheduled chapters are hidden from public count.
            ChapterCount = visibleChapters.Count,

            Chapters = listableChapters.Select(c => new ChapterNavItem
            {
                Id = c.Id,
                ChapterNumber = c.ChapterNumber,
                Title = c.Title,
                IsVisible = c.IsVisible,
                ScheduledAt = c.ScheduledAt,
                PremiumUnlocksAt = c.PremiumUnlocksAt
            }).ToList(),

            SimilarBooks = similar,
            UserLastChapterRead = clampedLastChapter,
            UserProgressPercent = progressPercent,

            // ── Author profile card ───────────────────────────────────────
            AuthorUserId = book.PublishedByUserId,
            AuthorDisplayName = authorDisplayName,
            AuthorProfilePhoto = authorPhoto,
            IsFollowingAuthor = isFollowing,
        };
    }

    // ── Similar books ─────────────────────────────────────────────────────
    public async Task<List<SimilarBookDto>> GetSimilarBooksAsync(
        Guid excludeBookId, string category, int limit = 6)
    {
        // Split genres and get all active books
        var genres = category?.Split(',').Select(g => g.Trim()).Where(g => !string.IsNullOrEmpty(g)).ToList() ?? new List<string>();
        var allBooks = await _bookRepo.GetAllActiveAsync();

        // Filter books that share at least one genre
        var filtered = allBooks
            .Where(b => b.Id != excludeBookId &&
                       genres.Any(g => b.Category != null && b.Category.Contains(g, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var result = new List<SimilarBookDto>();
        foreach (var b in filtered)
        {
            var avg = await _ratingRepo.GetAverageRatingAsync(b.Id);
            var count = await _ratingRepo.GetRatingCountAsync(b.Id);
            result.Add(new SimilarBookDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                Category = b.Category,
                CoverImage = b.CoverImage,
                IsFree = b.IsFree,
                AverageRating = Math.Round(avg, 1),
                RatingCount = count
            });
        }

        return result
            .OrderByDescending(b => b.RatingCount)
            .ThenByDescending(b => b.AverageRating)
            .Take(limit)
            .ToList();
    }

    // ── Bookmarks / favorites ─────────────────────────────────────────────
    public Task SaveScrollAsync(Guid userId, Guid bookId, int scrollPosition)
        => _bookmarkRepo.UpsertAsync(userId, bookId, scrollPosition);

    public Task<bool> ToggleFavoriteAsync(Guid userId, Guid bookId)
        => _favoriteRepo.ToggleAsync(userId, bookId);

    public Task<List<Favorite>> GetFavoritesAsync(Guid userId)
        => _favoriteRepo.GetByUserAsync(userId);

    // ── Recently read ─────────────────────────────────────────────────────
    public Task<List<BookRead>> GetRecentlyReadAsync(Guid userId, int take = 6)
        => _readRepo.GetRecentByUserAsync(userId, take);

    // ── Categories / single book ──────────────────────────────────────────
    public Task<List<string>> GetCategoriesAsync() => _bookRepo.GetCategoriesAsync();
    public Task<Book?> GetBookByIdAsync(Guid bookId) => _bookRepo.GetByIdAsync(bookId);

    // ── Subscription checks ───────────────────────────────────────────────
    public async Task<bool> IsSubscribedAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        return user?.SubscriptionTier == ReadEase.Enums.SubscriptionTier.Premium
            || user?.SubscriptionTier == ReadEase.Enums.SubscriptionTier.PremiumPlus;
    }

    public async Task<bool> IsPremiumPlusAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        return user?.SubscriptionTier == ReadEase.Enums.SubscriptionTier.PremiumPlus;
    }

    // ── Read counts ───────────────────────────────────────────────────────
    public Task<Dictionary<Guid, int>> GetReadCountsBatchAsync(IEnumerable<Guid> bookIds)
        => _readRepo.GetReadCountsBatchAsync(bookIds);

    // ── Top picks ─────────────────────────────────────────────────────────
    public Task<List<TopPickItem>> GetTopPicksAsync(int take = 5, int minVotes = 1)
        => _ratingRepo.GetTopRatedAsync(take, minVotes);

    // ── Ratings ───────────────────────────────────────────────────────────
    public async Task<RatingResponseDto> RateBookAsync(Guid userId, Guid bookId, int stars)
    {
        var existing = await _reviewRepo.GetUserReviewAsync(userId, bookId);
        await _ratingRepo.UpsertAsync(userId, bookId, stars);
        await _reviewRepo.UpsertAsync(userId, bookId, stars, existing?.Text);

        return new RatingResponseDto
        {
            Average = await _ratingRepo.GetAverageRatingAsync(bookId),
            Count = await _ratingRepo.GetRatingCountAsync(bookId)
        };
    }

    // ── Reviews ───────────────────────────────────────────────────────────
    public async Task<ReviewsResponseDto> GetReviewsAsync(Guid userId, Guid bookId)
    {
        var reviews = await _reviewRepo.GetByBookAsync(bookId);
        var myRating = await _ratingRepo.GetUserRatingAsync(userId, bookId);
        var dtos = new List<ReviewDto>();

        foreach (var r in reviews)
        {
            var comments = await _reviewRepo.GetCommentsAsync(r.Id);
            var commentDtos = new List<ReviewCommentDto>();

            foreach (var c in comments)
            {
                var allReplies = await _reviewRepo.GetRepliesAsync(c.Id);
                commentDtos.Add(new ReviewCommentDto
                {
                    Id = c.Id,
                    User = c.User.Name,
                    UserPhoto = c.User.ProfilePhoto,
                    UserRole = c.User.Role.ToString(),
                    Text = c.Text,
                    Ts = new DateTimeOffset(c.CreatedAt).ToUnixTimeMilliseconds(),
                    LikeCount = await _reviewRepo.GetCommentLikeCountAsync(c.Id),
                    LikedByMe = await _reviewRepo.IsCommentLikedByAsync(userId, c.Id),
                    Replies = BuildReplyTree(allReplies, parentId: c.Id)
                });
            }

            dtos.Add(new ReviewDto
            {
                Id = r.Id,
                User = r.User.Name,
                UserPhoto = r.User.ProfilePhoto,
                UserRole = r.User.Role.ToString(),
                Stars = r.Stars,
                Text = r.Text ?? "",
                Ts = new DateTimeOffset(r.UpdatedAt).ToUnixTimeMilliseconds(),
                AuthorReply = r.AuthorReply,
                AuthorRepliedTs = r.AuthorRepliedAt.HasValue
                                    ? new DateTimeOffset(r.AuthorRepliedAt.Value).ToUnixTimeMilliseconds()
                                    : null,
                LikeCount = await _reviewRepo.GetLikeCountAsync(r.Id),
                LikedByMe = await _reviewRepo.IsLikedByAsync(userId, r.Id),
                Comments = commentDtos
            });
        }

        return new ReviewsResponseDto
        {
            Success = true,
            Average = await _ratingRepo.GetAverageRatingAsync(bookId),
            Count = await _ratingRepo.GetRatingCountAsync(bookId),
            MyStars = myRating?.Stars ?? 0,
            Reviews = dtos
        };
    }

    public async Task<AddReviewResponseDto> AddReviewAsync(
        Guid userId, Guid bookId, int stars, string? text)
    {
        if (stars < 1 || stars > 5)
            return new AddReviewResponseDto { Success = false, Error = "Invalid star rating." };

        await _reviewRepo.UpsertAsync(userId, bookId, stars, text);
        await _ratingRepo.UpsertAsync(userId, bookId, stars);

        var result = await GetReviewsAsync(userId, bookId);
        return new AddReviewResponseDto
        {
            Success = true,
            Average = result.Average,
            Count = result.Count,
            Reviews = result.Reviews
        };
    }

    // ── Author reply ──────────────────────────────────────────────────────
    public async Task<bool> IsBookAuthorAsync(Guid userId, Guid bookId)
    {
        var book = await _bookRepo.GetByIdAsync(bookId);
        if (book is null) return false;
        var user = await _userRepo.GetByIdAsync(userId);
        return user?.Name?.Equals(book.Author, StringComparison.OrdinalIgnoreCase) == true;
    }

    public async Task<(bool Success, string? Error)> ReplyToReviewAsync(
        Guid userId, Guid reviewId, string replyText)
    {
        if (string.IsNullOrWhiteSpace(replyText))
            return (false, "Reply cannot be empty.");

        var review = await _reviewRepo.GetByIdAsync(reviewId);
        if (review is null) return (false, "Review not found.");

        var user = await _userRepo.GetByIdAsync(userId);
        var isAuthor = user?.Name?.Equals(review.Book.Author, StringComparison.OrdinalIgnoreCase) == true;
        if (!isAuthor) return (false, "You are not authorised to reply to this review.");

        var ok = await _reviewRepo.ReplyToReviewAsync(reviewId, replyText);
        return ok ? (true, null) : (false, "Could not save reply.");
    }

    public async Task<(bool Success, string? Error)> DeleteReplyAsync(Guid userId, Guid reviewId)
    {
        var review = await _reviewRepo.GetByIdAsync(reviewId);
        if (review is null) return (false, "Review not found.");

        var user = await _userRepo.GetByIdAsync(userId);
        var isAuthor = user?.Name?.Equals(review.Book.Author, StringComparison.OrdinalIgnoreCase) == true;
        if (!isAuthor) return (false, "You are not authorised to remove this reply.");

        await _reviewRepo.ReplyToReviewAsync(reviewId, null);
        return (true, null);
    }

    // ── Likes ─────────────────────────────────────────────────────────────
    public async Task<(bool Liked, int Count)> ToggleLikeAsync(Guid userId, Guid reviewId)
    {
        var liked = await _reviewRepo.ToggleLikeAsync(userId, reviewId);
        var count = await _reviewRepo.GetLikeCountAsync(reviewId);
        return (liked, count);
    }

    public async Task<(bool Liked, int Count)> ToggleCommentLikeAsync(Guid userId, Guid commentId)
    {
        var liked = await _reviewRepo.ToggleCommentLikeAsync(userId, commentId);
        var count = await _reviewRepo.GetCommentLikeCountAsync(commentId);
        return (liked, count);
    }

    // ── Comments ──────────────────────────────────────────────────────────
    public async Task<(bool Success, string? Error, ReviewCommentDto? Comment)>
        AddCommentAsync(Guid userId, Guid reviewId, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (false, "Comment cannot be empty.", null);

        var review = await _reviewRepo.GetByIdAsync(reviewId);
        if (review is null) return (false, "Review not found.", null);

        var c = await _reviewRepo.AddCommentAsync(userId, reviewId, text);

        return (true, null, new ReviewCommentDto
        {
            Id = c.Id,
            User = c.User.Name,
            UserPhoto = c.User.ProfilePhoto,
            UserRole = c.User.Role.ToString(),
            Text = c.Text,
            Ts = new DateTimeOffset(c.CreatedAt).ToUnixTimeMilliseconds(),
            LikeCount = 0,
            LikedByMe = false,
            Replies = []
        });
    }

    public async Task<(bool Success, string? Error, CommentReplyDto? Reply)>
        AddCommentReplyAsync(Guid userId, Guid reviewId, Guid commentId,
                             string text, string? replyingTo, Guid? parentReplyId = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (false, "Reply cannot be empty.", null);

        var review = await _reviewRepo.GetByIdAsync(reviewId);
        if (review is null) return (false, "Review not found.", null);

        var parentId = parentReplyId ?? commentId;
        var rep = await _reviewRepo.AddCommentReplyAsync(
            userId, reviewId, commentId, text, replyingTo, parentId);

        return (true, null, new CommentReplyDto
        {
            Id = rep.Id,
            User = rep.User.Name,
            UserPhoto = rep.User.ProfilePhoto,
            UserRole = rep.User.Role.ToString(),
            Text = rep.Text,
            ReplyingTo = rep.ReplyingTo,
            Ts = new DateTimeOffset(rep.CreatedAt).ToUnixTimeMilliseconds(),
            ParentCommentId = rep.ParentCommentId,
            Replies = []
        });
    }

    // ── Reading progress ──────────────────────────────────────────────────
    public Task LogChapterProgressAsync(Guid userId, Guid bookId, int chapterNumber)
        => _readRepo.LogChapterAsync(userId, bookId, chapterNumber);

    public Task MarkBookCompleteAsync(Guid userId, Guid bookId)
        => _readRepo.MarkCompleteAsync(userId, bookId);

    public async Task<bool> IsBookCompletedAsync(Guid userId, Guid bookId)
    {
        var record = await _readRepo.GetAsync(userId, bookId);
        return record?.IsCompleted ?? false;
    }

    public async Task<ReadingProgressViewModel> GetOngoingBooksAsync(Guid userId, bool premium)
    {
        var reads = await _readRepo.GetOngoingByUserAsync(userId);
        var (ongoingCount, completedCount) = await _readRepo.GetStatusCountsAsync(userId);

        var bookIds = reads.Select(r => r.Book.Id).ToList();
        var chapterCounts = await _chapterRepo.GetChapterCountsByBookIdsAsync(bookIds);

        return new ReadingProgressViewModel
        {
            IsSubscribed = premium,
            OngoingCount = ongoingCount,
            CompletedCount = completedCount,
            Books = reads.Select(r => new ReadingBookCardViewModel
            {
                Id = r.Book.Id,
                Title = r.Book.Title,
                Author = r.Book.Author,
                Category = r.Book.Category,
                CoverImage = r.Book.CoverImage,
                IsFree = r.Book.IsFree,
                IsCompleted = false,
                ReadAt = r.ReadAt,
                CompletedAt = r.CompletedAt,
                LastChapterRead = r.LastChapterRead,
                TotalChapters = chapterCounts.GetValueOrDefault(r.Book.Id, 0)
            }).ToList()
        };
    }

    public async Task<ReadingProgressViewModel> GetCompletedBooksAsync(Guid userId, bool premium)
    {
        var reads = await _readRepo.GetCompletedByUserAsync(userId);
        var (ongoingCount, completedCount) = await _readRepo.GetStatusCountsAsync(userId);

        return new ReadingProgressViewModel
        {
            IsSubscribed = premium,
            OngoingCount = ongoingCount,
            CompletedCount = completedCount,
            Books = reads.Select(r => new ReadingBookCardViewModel
            {
                Id = r.Book.Id,
                Title = r.Book.Title,
                Author = r.Book.Author,
                Category = r.Book.Category,
                CoverImage = r.Book.CoverImage,
                IsFree = r.Book.IsFree,
                IsCompleted = true,
                ReadAt = r.ReadAt,
                CompletedAt = r.CompletedAt
            }).ToList()
        };
    }

    public async Task<int> GetOngoingCountAsync(Guid userId)
    {
        var (ongoing, _) = await _readRepo.GetStatusCountsAsync(userId);
        return ongoing;
    }

    public async Task<int> GetCompletedCountAsync(Guid userId)
    {
        var (_, completed) = await _readRepo.GetStatusCountsAsync(userId);
        return completed;
    }

    // ── Featured sections ─────────────────────────────────────────────────
    public async Task<List<Book>> GetFeaturedTopPicksAsync(int count = 10)
    {
        // Top picks = highest rated books with most reads
        var books = await _bookRepo.GetAllActiveAsync();
        var readCounts = await _readRepo.GetReadCountsBatchAsync(books.Select(b => b.Id));
        return books
            .OrderByDescending(b => readCounts.GetValueOrDefault(b.Id, 0))
            .ThenByDescending(b => b.PublishedAt)
            .Take(count)
            .ToList();
    }

    public async Task<List<Book>> GetFeaturedNewReleasesAsync(int count = 10)
    {
        // New releases = most recently published
        var books = await _bookRepo.GetAllActiveAsync();
        return books
            .OrderByDescending(b => b.PublishedAt)
            .Take(count)
            .ToList();
    }

    public async Task<List<Book>> GetFeaturedRecommendedAsync(Guid userId, int count = 10)
    {
        // Get user's reading history and favorites
        var recentReads = await _readRepo.GetRecentByUserAsync(userId, 50);
        var ongoingReads = await _readRepo.GetOngoingByUserAsync(userId);
        var completedReads = await _readRepo.GetCompletedByUserAsync(userId);
        var favorites = await _favoriteRepo.GetByUserAsync(userId);
        var userBookIds = recentReads.Select(r => r.BookId)
            .Concat(ongoingReads.Select(r => r.BookId))
            .Concat(completedReads.Select(r => r.BookId))
            .Concat(favorites.Select(f => f.BookId))
            .ToHashSet();
        
        // Get categories the user likes
        var allBooks = await _bookRepo.GetAllActiveAsync();
        var userCategories = allBooks
            .Where(b => userBookIds.Contains(b.Id))
            .SelectMany(b => b.Category.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(c => c.Trim())
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(3)
            .ToList();

        // Get read counts for sorting
        var readCounts = await _readRepo.GetReadCountsBatchAsync(allBooks.Select(b => b.Id));

        // Recommend books in those categories that user hasn't read
        var recommended = allBooks
            .Where(b => !userBookIds.Contains(b.Id))
            .Where(b => userCategories.Any(uc => b.Category.Contains(uc)))
            .OrderByDescending(b => readCounts.GetValueOrDefault(b.Id, 0))
            .Take(count)
            .ToList();

        // If not enough recommendations, fill with popular books
        if (recommended.Count < count)
        {
            var popular = allBooks
                .Where(b => !userBookIds.Contains(b.Id) && !recommended.Contains(b))
                .OrderByDescending(b => readCounts.GetValueOrDefault(b.Id, 0))
                .Take(count - recommended.Count)
                .ToList();
            recommended.AddRange(popular);
        }

        return recommended;
    }

    // ── Private helpers ───────────────────────────────────────────────────
    private static List<ReviewCommentDto> BuildReplyTree(
        List<ReviewComment> all, Guid parentId)
    {
        return all
            .Where(r => r.ParentCommentId == parentId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new ReviewCommentDto
            {
                Id = r.Id,
                User = r.User.Name,
                UserPhoto = r.User.ProfilePhoto,
                UserRole = r.User.Role.ToString(),
                Text = r.Text,
                ReplyingTo = r.ReplyingTo,
                Ts = new DateTimeOffset(r.CreatedAt).ToUnixTimeMilliseconds(),
                ParentCommentId = r.ParentCommentId,
                Replies = BuildReplyTree(all, r.Id)
            })
            .ToList();
    }
}