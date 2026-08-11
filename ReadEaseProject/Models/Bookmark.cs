using ReadEase.Models;

namespace ReadEase.Models
{
    public class Bookmark
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid BookId { get; set; }
        public Book Book { get; set; } = null!;
        public int ScrollPosition { get; set; } = 0;
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

}
