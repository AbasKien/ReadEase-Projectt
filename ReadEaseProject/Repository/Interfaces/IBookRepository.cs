using ReadEase.Models;

namespace ReadEase.Repository.Interfaces;

public interface IBookRepository
{
    Task<List<Book>> GetAllActiveAsync();
    Task<List<Book>> GetFreeAsync();
    Task<List<Book>> SearchAsync(string? title, string? category);
    Task<Book?> GetByIdAsync(Guid id);
    Task<Book?> GetDeletedBookByIdAsync(Guid id);
    Task<List<Book>> GetDeletedAsync();
    Task<List<Book>> GetByAuthorUserIdAsync(Guid userId);
    Task<List<Book>> GetDeletedByAuthorAsync(Guid userId);
    Task<List<string>> GetCategoriesAsync();
    Task<List<Book>> GetPendingAsync();                          // ✅ NEW
    Task<Book?> GetAnyByIdAsync(Guid id);                       // ✅ NEW — includes pending/rejected
    Task CreateAsync(Book book);
    Task UpdateAsync(Book book);
    Task HardDeleteAsync(Guid id);
    Task UpdateAuthorNameAsync(Guid authorUserId, string newName);
}