using System.ComponentModel.DataAnnotations;

namespace ReadEase.ViewModels.Subscribe
{
    public class RejectSubscriptionViewModel
    {
        [Required]
        public Guid RequestId { get; set; }
        [Required(ErrorMessage = "Rejection reason is required.")]
        public string Reason { get; set; } = string.Empty;
    }
}
