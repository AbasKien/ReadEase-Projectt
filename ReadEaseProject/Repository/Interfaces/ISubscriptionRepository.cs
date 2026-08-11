using ReadEase.Models;

namespace ReadEase.Repository.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<SubscriptionRequest?> GetLatestByUserAsync(Guid userId);
        Task<List<SubscriptionRequest>> GetAllWithUserAsync();
        Task<List<SubscriptionRequest>> GetDeletedAsync();
        Task CreateAsync(SubscriptionRequest request);
        Task UpdateAsync(SubscriptionRequest request);
    }
}
