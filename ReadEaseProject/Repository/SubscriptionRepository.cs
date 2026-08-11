using ReadEase.Data;
using ReadEase.Enums;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ReadEase.Repository
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly AppDbContext _db;
        public SubscriptionRepository(AppDbContext db) => _db = db;

        public async Task<SubscriptionRequest?> GetLatestByUserAsync(Guid userId)
            => await _db.SubscriptionRequests
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SubmittedAt)
                .FirstOrDefaultAsync();

        public async Task<List<SubscriptionRequest>> GetAllWithUserAsync()
            => await _db.SubscriptionRequests
                .Include(s => s.User)
                .OrderByDescending(s => s.SubmittedAt).ToListAsync();

        public async Task<List<SubscriptionRequest>> GetDeletedAsync()
            => await _db.SubscriptionRequests
                .Include(s => s.User)
                .Where(s => s.Status == SubscriptionStatus.Rejected)
                .OrderByDescending(s => s.SubmittedAt).ToListAsync();

        public async Task CreateAsync(SubscriptionRequest req)
        { await _db.SubscriptionRequests.AddAsync(req); await _db.SaveChangesAsync(); }

        public async Task UpdateAsync(SubscriptionRequest req)
        { _db.SubscriptionRequests.Update(req); await _db.SaveChangesAsync(); }
    }
}
