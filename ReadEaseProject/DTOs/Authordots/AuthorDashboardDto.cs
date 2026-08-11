namespace ReadEase.DTOs.Authordots
{
    public class AuthorDashboardDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalBooks { get; set; }
        public int TotalReads { get; set; }
    }
}
