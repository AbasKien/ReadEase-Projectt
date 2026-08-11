namespace ReadEase.DTOs.Bookdtos;

public class CommentReplyDto
{
    public Guid Id { get; set; }
    public string User { get; set; } = "";
    public string? UserPhoto { get; set; }
    public string UserRole { get; set; } = "";
    public string Text { get; set; } = "";
    public string? ReplyingTo { get; set; }
    public long Ts { get; set; }
    public Guid? ParentCommentId { get; set; }
    public List<CommentReplyDto> Replies { get; set; } = new();
}