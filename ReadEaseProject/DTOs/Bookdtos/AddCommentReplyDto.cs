namespace ReadEase.DTOs.Bookdtos;

public class AddCommentReplyDto
{
    public Guid CommentId { get; set; }
    public Guid ReviewId { get; set; }
    public string Text { get; set; } = "";
    public string? ReplyingTo { get; set; }
    public Guid? ParentReplyId { get; set; }
}