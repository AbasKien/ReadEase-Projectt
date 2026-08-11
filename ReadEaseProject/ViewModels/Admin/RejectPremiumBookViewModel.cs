using System.ComponentModel.DataAnnotations;

namespace ReadEase.ViewModels.Admin;

public class RejectPremiumBookViewModel
{
    [Required]
    public Guid BookId { get; set; }

    [Required(ErrorMessage = "Please provide a rejection reason.")]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}