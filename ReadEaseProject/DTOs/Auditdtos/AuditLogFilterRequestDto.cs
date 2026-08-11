namespace ReadEase.DTOs.Auditdtos
{
    public class AuditLogFilterRequestDto
    {
        public string? UserId { get; set; }
        public string? Role { get; set; }
        public string? ActionFilter { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
