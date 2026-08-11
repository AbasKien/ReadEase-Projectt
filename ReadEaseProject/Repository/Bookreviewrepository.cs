using Microsoft.EntityFrameworkCore;
using ReadEase.Data;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;

namespace ReadEase.Repository;

public class BookReviewRepository : IBookReviewRepository
{
    private readonly AppDbContext _db;
    public BookReviewRepository(AppDbContext db) => _db = db;

    // ── reviews ───────────────────────────────────────────────────────────

    public async Task<BookReview?> GetUserReviewAsync(Guid userId, Guid bookId)
        => await _db.BookReviews
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);

    public async Task<List<BookReview>> GetByBookAsync(Guid bookId)
        => await _db.BookReviews
                    .Include(r => r.User)
                    .Where(r => r.BookId == bookId)
                    .OrderByDescending(r => r.UpdatedAt)
                    .ToListAsync();

    public async Task UpsertAsync(Guid userId, Guid bookId, int stars, string? text)
    {
        var existing = await _db.BookReviews
                                .FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId);
        if (existing is null)
        {
            await _db.BookReviews.AddAsync(new BookReview
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BookId = bookId,
                Stars = stars,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Stars = stars;
            existing.Text = text ?? existing.Text;
            existing.UpdatedAt = DateTime.UtcNow;
            _db.BookReviews.Update(existing);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<BookReview?> GetByIdAsync(Guid reviewId)
        => await _db.BookReviews
                    .Include(r => r.User)
                    .Include(r => r.Book)
                    .FirstOrDefaultAsync(r => r.Id == reviewId);

    public async Task<bool> ReplyToReviewAsync(Guid reviewId, string? replyText)
    {
        var review = await _db.BookReviews.FindAsync(reviewId);
        if (review is null) return false;
        review.AuthorReply = string.IsNullOrWhiteSpace(replyText) ? null : replyText.Trim();
        review.AuthorRepliedAt = string.IsNullOrWhiteSpace(replyText) ? null : DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── review likes ──────────────────────────────────────────────────────

    public async Task<bool> ToggleLikeAsync(Guid userId, Guid reviewId)
    {
        var existing = await _db.ReviewLikes
                                .FirstOrDefaultAsync(l => l.UserId == userId && l.ReviewId == reviewId);
        if (existing is null)
        {
            _db.ReviewLikes.Add(new ReviewLike
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReviewId = reviewId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return true;
        }
        _db.ReviewLikes.Remove(existing);
        await _db.SaveChangesAsync();
        return false;
    }

    public Task<int> GetLikeCountAsync(Guid reviewId)
        => _db.ReviewLikes.CountAsync(l => l.ReviewId == reviewId);

    public Task<bool> IsLikedByAsync(Guid userId, Guid reviewId)
        => _db.ReviewLikes.AnyAsync(l => l.UserId == userId && l.ReviewId == reviewId);

    // ── comment likes ─────────────────────────────────────────────────────

    public async Task<bool> ToggleCommentLikeAsync(Guid userId, Guid commentId)
    {
        var existing = await _db.ReviewCommentLikes
                                .FirstOrDefaultAsync(l => l.UserId == userId && l.CommentId == commentId);
        if (existing is null)
        {
            _db.ReviewCommentLikes.Add(new ReviewCommentLike
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CommentId = commentId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return true;
        }
        _db.ReviewCommentLikes.Remove(existing);
        await _db.SaveChangesAsync();
        return false;
    }

    public Task<int> GetCommentLikeCountAsync(Guid commentId)
        => _db.ReviewCommentLikes.CountAsync(l => l.CommentId == commentId);

    public Task<bool> IsCommentLikedByAsync(Guid userId, Guid commentId)
        => _db.ReviewCommentLikes.AnyAsync(l => l.UserId == userId && l.CommentId == commentId);

    // ── comments ──────────────────────────────────────────────────────────

    public async Task<ReviewComment> AddCommentAsync(Guid userId, Guid reviewId, string text)
    {
        var c = new ReviewComment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ReviewId = reviewId,
            Text = text.Trim(),
            ParentCommentId = null,   // root comment — no parent
            CreatedAt = DateTime.UtcNow
        };
        _db.ReviewComments.Add(c);
        await _db.SaveChangesAsync();

        return await _db.ReviewComments
                        .Include(x => x.User)
                        .FirstAsync(x => x.Id == c.Id);
    }

    // Returns only root-level comments (ParentCommentId == null) for a review
    public async Task<List<ReviewComment>> GetCommentsAsync(Guid reviewId)
        => await _db.ReviewComments
                    .Include(c => c.User)
                    .Where(c => c.ReviewId == reviewId && c.ParentCommentId == null)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

    // ── comment replies ───────────────────────────────────────────────────

    // rootCommentId   = top-level comment this thread belongs to (used for ReviewId scope)
    // parentCommentId = immediate parent — either the root comment or a nested reply
    //                   This is what creates the branch in the tree.
    public async Task<ReviewComment> AddCommentReplyAsync(
        Guid userId, Guid reviewId, Guid rootCommentId,
        string text, string? replyingTo, Guid parentCommentId)
    {
        var reply = new ReviewComment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ReviewId = reviewId,
            ParentCommentId = parentCommentId,   // ← the branch point
            Text = text.Trim(),
            ReplyingTo = replyingTo,
            CreatedAt = DateTime.UtcNow
        };
        _db.ReviewComments.Add(reply);
        await _db.SaveChangesAsync();

        return await _db.ReviewComments
                        .Include(x => x.User)
                        .FirstAsync(x => x.Id == reply.Id);
    }

    // Returns ALL descendants of rootCommentId as a flat list.
    // LibraryService.BuildReplyTree() assembles this into the recursive tree.
    public async Task<List<ReviewComment>> GetRepliesAsync(Guid rootCommentId)
    {
        var root = await _db.ReviewComments
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.Id == rootCommentId);

        if (root is null) return [];

        // Load every non-root comment in the same review
        var allInReview = await _db.ReviewComments
                                   .AsNoTracking()
                                   .Include(c => c.User)
                                   .Where(c => c.ReviewId == root.ReviewId
                                            && c.ParentCommentId != null)
                                   .OrderBy(c => c.CreatedAt)
                                   .ToListAsync();

        // Return only descendants of rootCommentId (not siblings from other threads)
        return CollectDescendants(allInReview, rootCommentId);
    }

    // Recursively collects all children, grandchildren, etc. from a flat list
    private static List<ReviewComment> CollectDescendants(
        List<ReviewComment> all, Guid parentId)
    {
        var result = new List<ReviewComment>();
        foreach (var child in all.Where(c => c.ParentCommentId == parentId))
        {
            result.Add(child);
            result.AddRange(CollectDescendants(all, child.Id));
        }
        return result;
    }
}