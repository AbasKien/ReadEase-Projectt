namespace ReadEase.Models;

public class PostLike
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }            // FK → UserPost.Id
    public Guid UserId { get; set; }            // FK → User.Id (liker)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public UserPost? Post { get; set; }
    public User? User { get; set; }
}