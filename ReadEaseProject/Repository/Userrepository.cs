using Microsoft.EntityFrameworkCore;
using ReadEase.Data;
using ReadEase.Enums;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;

namespace ReadEase.Repository;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

    public async Task<User?> GetByExternalIdAsync(string provider, string externalId)
        => await _context.Users
            .FirstOrDefaultAsync(u => u.Provider == provider && u.ExternalId == externalId);

    public async Task<User?> GetByNameAsync(string name)
        => await _context.Users.FirstOrDefaultAsync(u => u.Name == name);

    public async Task<bool> EmailExistsAsync(string email)
        => await _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());

    public async Task<List<User>> GetAllAsync()
        => await _context.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();

    // ✅ ADDED — returns all admin users for notification fanout
    public async Task<List<User>> GetAdminUsersAsync()
        => await _context.Users
            .Where(u => u.Role == UserRole.Admin)
            .ToListAsync();

    public async Task CreateAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    // ✅ ADDED — permanent delete by ID, used by AdminController.PermanentDeleteUser
    public async Task HardDeleteAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is not null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}