using ReadEase.Enums;

namespace ReadEase.Models
{
    public class SubscriptionRequest
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string? ReceiptPhoto { get; set; }
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
        public string? Reason { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// The tier the user is requesting. Stored so the admin knows
        /// what to approve (Premium vs PremiumPlus).
        /// </summary>
        public SubscriptionTier RequestedTier { get; set; } = SubscriptionTier.Premium;
    }
}