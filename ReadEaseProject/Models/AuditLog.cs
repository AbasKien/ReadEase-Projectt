namespace ReadEase.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? RecordAffected { get; set; }
        public string? IpAddress { get; set; }
        public string? Device { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}