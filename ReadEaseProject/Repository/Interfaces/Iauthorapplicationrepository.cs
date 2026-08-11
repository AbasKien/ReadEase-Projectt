using ReadEase.Enums;
using ReadEase.Models;

namespace ReadEase.Repository.Interfaces;

public interface IAuthorApplicationRepository
{
    Task<AuthorApplication?> GetByIdAsync(Guid id);
    Task<AuthorApplication?> GetPendingByUserIdAsync(Guid userId);
    Task<bool> HasPendingApplicationAsync(Guid userId);
    Task<List<AuthorApplication>> GetAllWithUserAsync();
    Task<List<AuthorApplication>> GetByStatusAsync(ApplicationStatus status);
    Task CreateAsync(AuthorApplication application);
    Task UpdateAsync(AuthorApplication application);
}