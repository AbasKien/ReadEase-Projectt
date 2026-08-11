using ReadEase.Models;

namespace ReadEase.Repository.Interfaces { 

    public interface IBookmarkRepository
{
    Task<Bookmark?> GetAsync(Guid userId, Guid bookId);
    Task UpsertAsync(Guid userId, Guid bookId, int scrollPosition);
}
}