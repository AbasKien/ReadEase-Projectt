using ReadEase.Data;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ReadEase.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _db;
        public NotificationRepository(AppDbContext db) => _db = db;

        public async Task<List<Notification>> GetByUserAsync(Guid userId, int take = 10)
            => await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take).ToListAsync();

        public async Task<int> GetUnreadCountAsync(Guid userId)
            => await _db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task CreateAsync(Notification n)
        { await _db.Notifications.AddAsync(n); await _db.SaveChangesAsync(); }

        public async Task MarkAllReadAsync(Guid userId)
        {
            var unread = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();
        }

        public async Task MarkReadAsync(Guid notificationId)
        {
            var n = await _db.Notifications.FindAsync(notificationId);
            if (n is not null) { n.IsRead = true; await _db.SaveChangesAsync(); }
        }
    }
}
