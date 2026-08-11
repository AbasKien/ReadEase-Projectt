using ReadEase.Models;

namespace ReadEase.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendAsync(Guid userId, string message, string? link = null);
        Task<List<Notification>> GetForUserAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAllReadAsync(Guid userId);
        Task MarkReadAsync(Guid notificationId);
    }
}
