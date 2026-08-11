using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ReadEase.Hubs;

/// <summary>
/// Real-time audit log hub.
/// Only admins can connect — enforced by [Authorize(Roles = "Admin")].
/// Every new log entry is broadcast to the "admins" group.
/// </summary>
[Authorize(Roles = "Admin")]
public class AuditHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins");
        await base.OnDisconnectedAsync(exception);
    }
}