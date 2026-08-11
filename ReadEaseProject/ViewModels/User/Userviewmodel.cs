namespace ReadEase.ViewModels.User;

public class UserViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ProfilePhoto { get; set; }
    public bool IsSubscribed { get; set; }
}