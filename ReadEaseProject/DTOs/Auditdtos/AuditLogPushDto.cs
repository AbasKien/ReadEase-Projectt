namespace ReadEase.DTOs.Auditdtos
{
    public class AuditLogPushDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? RecordAffected { get; set; }
        public string? IpAddress { get; set; }
        public string? Device { get; set; }
    }
}
