namespace ReadEase.ViewModels.AuditLog
{
    public class AuditLogFilterViewModel
    {
        public string? UserId { get; set; }
        public string? Role { get; set; }
        public string? Action { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public List<AuditLogViewModel> Logs { get; set; } = new();
    }
}
