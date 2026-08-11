namespace ReadEase.Models;

/// <summary>
/// A community post that appears on a user's profile wall.
/// AuthorId  = who wrote the post (visitor or owner).
/// WallOwnerId = whose profile wall it appears on (always the profile being viewed).
/// </summary>
public class UserPost
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuthorId { get; set; }          // FK → User.Id (writer)
    public Guid WallOwnerId { get; set; }        // FK → User.Id (profile owner)
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public User? Author { get; set; }
    public User? WallOwner { get; set; }
    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
}