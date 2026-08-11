namespace ReadEase.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalAuthors { get; set; }
        public int PendingApplications { get; set; }
        public int PendingSubscriptions { get; set; }
        public int PendingPremiumBooks { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
    }
}
