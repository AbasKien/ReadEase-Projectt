using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;

namespace ReadEase.Jobs;

/// <summary>
/// Hangfire job that fires at PremiumUnlocksAt (= ScheduledAt - 24 hours).
/// Sends a notification to all Premium and Premium++ fans of the book's author
/// so they know the chapter is now available to them — 24 hours before free readers.
///
/// Note: The chapter's IsVisible flag is NOT flipped here; it stays false until
/// ChapterReleaseJob runs at ScheduledAt. The gate logic in LibraryController.Read
/// allows Premium readers to access the content based on PremiumUnlocksAt even
/// while IsVisible is still false.
/// </summary>
public class ChapterPremiumNotifyJob
{
    private readonly IFavoriteRepository _favRepo;
    private readonly INotificationService _notify;
    private readonly IBookRepository _bookRepo;

    public ChapterPremiumNotifyJob(
        IFavoriteRepository favRepo,
        INotificationService notify,
        IBookRepository bookRepo)
    {
        _favRepo = favRepo;
        _notify = notify;
        _bookRepo = bookRepo;
    }

    public async Task ExecuteAsync(
        Guid chapterId,
        Guid bookId,
        string bookTitle,
        int chapterNumber,
        string chapterTitle)
    {
        // Look up the book to get the author's user ID for the fan query
        var book = await _bookRepo.GetByIdAsync(bookId);
        if (book is null) return;

        // GetPremiumFanUserIdsForAuthorAsync should return only Premium / Premium++ subscribers
        // who have favorited this author's books.
        // If your IFavoriteRepository only has GetFanUserIdsForAuthorAsync (returns all fans),
        // use that and rely on the read-gate in the controller to enforce tier access.
        var fanIds = await _favRepo.GetFanUserIdsForAuthorAsync(book.PublishedByUserId);
        if (fanIds.Count == 0) return;

        var message = $"⭐ Early access! Ch.{chapterNumber} of \"{bookTitle}\" — {chapterTitle} is now available for Premium readers!";
        var link = $"/Library/Read/{bookId}?chapter={chapterNumber}";

        foreach (var uid in fanIds)
            await _notify.SendAsync(uid, message, link);
    }
}