using ReadEase.ViewModels.Admin;

namespace ReadEase.Services;

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardStatsAsync();

    // ── Pending premium book approvals ────────────────────────────────────
    Task<List<PendingBookViewModel>> GetPendingBooksAsync();
    Task ApprovePremiumBookAsync(Guid bookId, Guid adminId);
    Task RejectPremiumBookAsync(Guid bookId, string reason, Guid adminId);

    // ── User management ───────────────────────────────────────────────────
    Task<List<UserManagementViewModel>> GetAllUsersAsync();
    Task ToggleUserActiveAsync(Guid userId);
}