using ReadEase.Models;
using ReadEase.ViewModels.Library;

namespace ReadEase.Area.Mobile;

/// <summary>
/// Thin mapping helpers used only in the Mobile area.
/// Keeps MobileController clean and avoids touching existing web services.
/// </summary>
public static class MobileMapper
{
    /// <summary>Maps a <see cref="Book"/> entity to a <see cref="BookCardViewModel"/>.</summary>
    public static BookCardViewModel ToCard(Book b) => new()
    {
        Id = b.Id,
        Title = b.Title,
        Author = b.Author,           // FIX: was b.AuthorName — Book.Author is the correct property
        Category = b.Category,
        CoverImage = b.CoverImage,
        IsFree = b.IsFree,
        PublishedAt = b.PublishedAt,
        IsStoryComplete = b.IsStoryComplete    // FIX: add IsStoryComplete to BookCardViewModel if missing (see note)
    };
}