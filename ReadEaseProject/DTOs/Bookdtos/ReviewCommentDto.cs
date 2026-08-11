using ReadEase.DTOs.Bookdtos;

namespace ReadEase.DTOs.Book;

public class ReviewCommentDto
{
    public Guid Id { get; set; }
    public string User { get; set; } = "";
    public string? UserPhoto { get; set; }
    public string UserRole { get; set; } = "";
    public string Text { get; set; } = "";
    public long Ts { get; set; }
    public int LikeCount { get; set; }
    public bool LikedByMe { get; set; }
    public string? ReplyingTo { get; set; }          // ← needed by BuildReplyTree
    public Guid? ParentCommentId { get; set; }        // ← needed by BuildReplyTree
    public List<ReviewCommentDto> Replies { get; set; } = [];  // ← self-referencing, NOT CommentReplyDto
}