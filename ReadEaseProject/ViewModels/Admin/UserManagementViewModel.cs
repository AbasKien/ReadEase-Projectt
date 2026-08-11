namespace ReadEase.ViewModels.Admin
{
    public class UserManagementViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
