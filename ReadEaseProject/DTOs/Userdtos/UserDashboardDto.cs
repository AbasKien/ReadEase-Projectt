namespace ReadEase.DTOs.Userdtos
{
    public class UserDashboardDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public bool IsSubscribed { get; set; }
        public int BooksRead { get; set; }
        public int FavoritesCount { get; set; }
    }
}
