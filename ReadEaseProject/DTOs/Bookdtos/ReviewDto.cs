using ReadEase.DTOs.Book;
using ReadEase.DTOs.Bookdtos;

namespace ReadEase.DTOs.Book;

public class ReviewDto
{
    public Guid Id { get; set; }
    public string User { get; set; } = "";
    public string? UserPhoto { get; set; }
    public string UserRole { get; set; } = "User";
    public int Stars { get; set; }
    public string Text { get; set; } = "";
    public long Ts { get; set; }
    public string? AuthorReply { get; set; }
    public long? AuthorRepliedTs { get; set; }
    public int LikeCount { get; set; }
    public bool LikedByMe { get; set; }
    public List<ReviewCommentDto> Comments { get; set; } = [];
}