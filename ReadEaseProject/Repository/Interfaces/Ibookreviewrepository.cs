using ReadEase.Models;

namespace ReadEase.Repository.Interfaces;

public interface IBookReviewRepository
{
    // ── reviews ───────────────────────────────────────────────────────────
    Task<BookReview?> GetUserReviewAsync(Guid userId, Guid bookId);
    Task<List<BookReview>> GetByBookAsync(Guid bookId);
    Task UpsertAsync(Guid userId, Guid bookId, int stars, string? text);
    Task<BookReview?> GetByIdAsync(Guid reviewId);
    Task<bool> ReplyToReviewAsync(Guid reviewId, string? replyText);
    // ── review likes ──────────────────────────────────────────────────────
    Task<bool> ToggleLikeAsync(Guid userId, Guid reviewId);
    Task<int> GetLikeCountAsync(Guid reviewId);
    Task<bool> IsLikedByAsync(Guid userId, Guid reviewId);
    // ── comment likes ─────────────────────────────────────────────────────
    Task<bool> ToggleCommentLikeAsync(Guid userId, Guid commentId);
    Task<int> GetCommentLikeCountAsync(Guid commentId);
    Task<bool> IsCommentLikedByAsync(Guid userId, Guid commentId);
    // ── comments ──────────────────────────────────────────────────────────
    Task<ReviewComment> AddCommentAsync(Guid userId, Guid reviewId, string text);
    // Returns only root-level comments (ParentCommentId == null) for a review
    Task<List<ReviewComment>> GetCommentsAsync(Guid reviewId);
    // ── comment replies ───────────────────────────────────────────────────
    // rootCommentId  = the top-level comment this thread belongs to
    // parentCommentId = the immediate parent (root comment OR a nested reply)
    Task<ReviewComment> AddCommentReplyAsync(
        Guid userId, Guid reviewId, Guid rootCommentId,
        string text, string? replyingTo, Guid parentCommentId);
    // Returns ALL descendants of rootCommentId flat — service builds the tree
    Task<List<ReviewComment>> GetRepliesAsync(Guid rootCommentId);
}