namespace ReadEase.ViewModels.AuditLog;

public class AuditLogViewModel
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }  // resolved from Users table
    public string Role { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? RecordAffected { get; set; }
    public string? IpAddress { get; set; }
    public string? Device { get; set; }
    public DateTime CreatedAt { get; set; }
}