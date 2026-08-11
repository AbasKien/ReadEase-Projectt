using ReadEase.Enums;

namespace ReadEase.ViewModels.Subscribe
{
    public class SubscribeViewModel
    {
        public SubscriptionStatus? CurrentStatus { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? SubmittedAt { get; set; }

        /// <summary>
        /// The tier the user selected on their latest request.
        /// </summary>
        public SubscriptionTier RequestedTier { get; set; } = SubscriptionTier.Premium;

        /// <summary>
        /// The tier the user actually holds right now (from User.SubscriptionTier).
        /// </summary>
        public SubscriptionTier CurrentTier { get; set; } = SubscriptionTier.Free;

        public bool IsSubscribed { get; set; }
    }
}