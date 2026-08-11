using Hangfire.Dashboard;

namespace ReadEase.Jobs;

/// <summary>
/// Restricts the Hangfire dashboard to users with the Admin role.
/// </summary>
public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User.Identity?.IsAuthenticated == true
            && http.User.IsInRole("Admin");
    }
}