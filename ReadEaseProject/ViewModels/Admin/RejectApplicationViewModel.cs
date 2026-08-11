using System.ComponentModel.DataAnnotations;

namespace ReadEase.ViewModels.Admin
{
    public class RejectApplicationViewModel
    {
        [Required]
        public Guid ApplicationId { get; set; }

        [Required(ErrorMessage = "Rejection reason is required.")]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }    
    
}
