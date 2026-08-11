using ReadEase.Models;

namespace ReadEase.Repository.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByExternalIdAsync(string provider, string externalId);
    Task<User?> GetByNameAsync(string name);
    Task<bool> EmailExistsAsync(string email);
    Task<List<User>> GetAllAsync();
    Task<List<User>> GetAdminUsersAsync(); // ✅ ADDED — used to notify admins on premium requests
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
    Task HardDeleteAsync(Guid userId);     // ✅ ADDED — permanent delete by ID (used by Admin)
}