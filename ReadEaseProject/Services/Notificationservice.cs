using Microsoft.AspNetCore.SignalR;
using ReadEase.Hubs;
using ReadEase.Models;
using ReadEase.Repository;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;

namespace ReadEase.Services;


public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(
        INotificationRepository repo,
        IHubContext<NotificationHub> hub)
    {
        _repo = repo;
        _hub = hub;
    }

    public async Task SendAsync(Guid userId, string message, string? link = null)
    {
        // 1. Persist to DB
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            Link = link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.CreateAsync(notification);

        // 2. Get updated unread count for the badge
        var unreadCount = await _repo.GetUnreadCountAsync(userId);

        // 3. Push in real-time via SignalR to the user's personal group
        await _hub.Clients
            .Group($"user-{userId}")
            .SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                message = notification.Message,
                link = notification.Link,
                isRead = notification.IsRead,
                createdAt = notification.CreatedAt,
                unreadCount = unreadCount
            });
    }

    public async Task<List<Notification>> GetForUserAsync(Guid userId)
        => await _repo.GetByUserAsync(userId);

    public async Task<int> GetUnreadCountAsync(Guid userId)
        => await _repo.GetUnreadCountAsync(userId);

    public async Task MarkAllReadAsync(Guid userId)
    {
        await _repo.MarkAllReadAsync(userId);

        // Push updated count (0) in real-time
        await _hub.Clients
            .Group($"user-{userId}")
            .SendAsync("UpdateUnreadCount", 0);
    }

    public async Task MarkReadAsync(Guid notificationId)
        => await _repo.MarkReadAsync(notificationId);
}