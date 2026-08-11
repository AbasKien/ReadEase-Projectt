using Microsoft.EntityFrameworkCore;
using ReadEase.Data;
using ReadEase.Enums;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;

namespace ReadEase.Repository;

public class AuthorApplicationRepository : IAuthorApplicationRepository
{
    private readonly AppDbContext _context;

    public AuthorApplicationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AuthorApplication?> GetByIdAsync(Guid id)
        => await _context.AuthorApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<AuthorApplication?> GetPendingByUserIdAsync(Guid userId)
        => await _context.AuthorApplications
            .FirstOrDefaultAsync(a => a.UserId == userId
                                   && a.Status == ApplicationStatus.Pending);

    public async Task<bool> HasPendingApplicationAsync(Guid userId)
        => await _context.AuthorApplications
            .AnyAsync(a => a.UserId == userId && a.Status == ApplicationStatus.Pending);

    public async Task<List<AuthorApplication>> GetAllWithUserAsync()
        => await _context.AuthorApplications
            .Include(a => a.User)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

    public async Task<List<AuthorApplication>> GetByStatusAsync(ApplicationStatus status)
        => await _context.AuthorApplications
            .Include(a => a.User)
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

    public async Task CreateAsync(AuthorApplication application)
    {
        await _context.AuthorApplications.AddAsync(application);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AuthorApplication application)
    {
        _context.AuthorApplications.Update(application);
        await _context.SaveChangesAsync();
    }
}