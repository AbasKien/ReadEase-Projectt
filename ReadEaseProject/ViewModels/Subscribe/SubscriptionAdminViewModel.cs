using ReadEase.Enums;

namespace ReadEase.ViewModels.Subscribe
{
    public class SubscriptionAdminViewModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? ReceiptPhoto { get; set; }
        public SubscriptionStatus Status { get; set; }
        public string? Reason { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
