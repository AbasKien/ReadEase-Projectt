using ReadEase.ViewModels.Author;

namespace ReadEase.Services.Interfaces;

public interface IAuthorService
{
    Task<AuthorDashboardViewModel> GetDashboardAsync(Guid userId, string userName, string userEmail);
    Task<ManageBooksViewModel> GetMyBooksAsync(Guid userId);
    Task<List<ManageBookItem>> GetArchiveAsync(Guid userId);
    Task<PublishBookViewModel?> GetBookForEditAsync(Guid bookId, Guid userId);
    Task<(bool IsPending, bool WasPromoted)> PublishBookAsync(Guid userId, string authorName, PublishBookViewModel vm, string webRootPath);
    Task<bool> EditBookAsync(Guid userId, PublishBookViewModel vm, string webRootPath);
    Task ArchiveBookAsync(Guid bookId, Guid userId);
    Task RestoreBookAsync(Guid bookId, Guid userId);
    Task PermanentDeleteAsync(Guid bookId, Guid userId);
    Task<bool> ToggleStoryCompleteAsync(Guid bookId, Guid userId); // ✅ NEW — returns new state
}