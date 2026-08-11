namespace ReadEase.DTOs.Subscriptiondtos
{
    public class SubscriptionStatusDto
    {
        public bool IsSubscribed { get; set; }
        public string Status { get; set; } = string.Empty;  // Pending | Approved | Rejected | None
        public string? Reason { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// The tier attached to this subscription request.
        /// </summary>
        public ReadEase.Enums.SubscriptionTier RequestedTier { get; set; }
            = ReadEase.Enums.SubscriptionTier.Premium;
    }
}