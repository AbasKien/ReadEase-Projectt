using ReadEase.Enums;

namespace ReadEase.ViewModels.Admin
{
    public class AuthorApplicationViewModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime AppliedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
