namespace ReadEase.DTOs.Subscriptiondtos
{
    public class ReviewSubscriptionRequestDto
    {
        public Guid RequestId { get; set; }
        public bool Approve { get; set; }
        public string? Reason { get; set; }
    }
}
