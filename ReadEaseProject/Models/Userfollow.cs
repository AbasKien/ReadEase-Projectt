namespace ReadEase.Models;

/// <summary>
/// Follow relationship: FollowerId follows FollowedId.
/// </summary>
public class UserFollow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FollowerId { get; set; }        // FK → User.Id (who is following)
    public Guid FollowedId { get; set; }        // FK → User.Id (who is being followed)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Follower { get; set; }
    public User? Followed { get; set; }
}