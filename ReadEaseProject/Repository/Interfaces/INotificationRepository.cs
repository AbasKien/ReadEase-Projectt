using ReadEase.Models;

namespace ReadEase.Repository.Interfaces
{

    public interface INotificationRepository
    {
        Task<List<Notification>> GetByUserAsync(Guid userId, int take = 10);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task CreateAsync(Notification notification);
        Task MarkAllReadAsync(Guid userId);
        Task MarkReadAsync(Guid notificationId);
    }
}
