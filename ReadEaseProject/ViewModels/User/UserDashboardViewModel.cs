using ReadEase.ViewModels.Library; // ← add this

namespace ReadEase.ViewModels.User
{
    public class UserDashboardViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public bool IsSubscribed { get; set; }
        public bool HasPendingApplication { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public int BooksRead { get; set; }
        public int FavoritesCount { get; set; }
        public List<RecentBookItem> RecentlyRead { get; set; } = new();
        public List<RecentBookItem> Favorites { get; set; } = new();
    }
}