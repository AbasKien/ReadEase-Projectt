using Hangfire;
using ReadEase.Enums;
using ReadEase.Services.Interfaces;

namespace ReadEase.Jobs;

/// <summary>
/// Hangfire background job that fires when a scheduled chapter's release time arrives.
///
/// Tier behaviour at release time:
///   Free        → chapter becomes visible, NO notification sent.
///               → 24-hr read-gate enforced in LibraryController.Read()
///   Premium     → chapter becomes visible, notification sent ("unlocks in 24 hrs").
///               → 24-hr read-gate enforced in LibraryController.Read()
///   Premium++   → chapter becomes visible, notification sent ("read now!").
///               → NO read-gate — can read immediately.
/// </summary>
public class ChapterReleaseJob
{
    private readonly IChapterService _chapterService;
    private readonly INotificationService _notifService;
    private readonly ISubscriptionService _subService;

    public ChapterReleaseJob(
        IChapterService chapterService,
        INotificationService notifService,
        ISubscriptionService subService)
    {
        _chapterService = chapterService;
        _notifService = notifService;
        _subService = subService;
    }

    /// <summary>
    /// Called by Hangfire at the scheduled release time.
    /// 1. Flips IsVisible = true and sets PublishedAt = UtcNow.
    /// 2. Fan-out tier-aware notifications to all followers.
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(
        Guid chapterId,
        Guid bookId,
        string bookTitle,
        int chapterNumber,
        string chapterTitle)
    {
        // Step 1 — Make the chapter visible
        // Sets IsVisible = true, PublishedAt = UtcNow in ChapterService
        await _chapterService.PublishScheduledChapterAsync(chapterId);

        // Step 2 — Get all followers of this book with their current tier
        var followers = await _subService.GetBookFollowersAsync(bookId);
        if (followers.Count == 0) return;

        var readLink = $"/Library/Read/{bookId}?chapter={chapterNumber}";

        foreach (var follower in followers)
        {
            switch (follower.Tier)
            {
                // ── Premium++ ─────────────────────────────────────────────
                // Notified immediately. No read-gate — can read right now.
                case SubscriptionTier.PremiumPlus:
                    await _notifService.SendAsync(
                        follower.UserId,
                        $"📖 New chapter is live! \"{bookTitle}\" — Ch.{chapterNumber}: {chapterTitle}. Read now!",
                        readLink);
                    break;

                // ── Premium ───────────────────────────────────────────────
                // Notified, but must wait 24 hrs before reading.
                // LibraryController.Read() enforces the gate using PublishedAt.
                case SubscriptionTier.Premium:
                    await _notifService.SendAsync(
                        follower.UserId,
                        $"🔔 New chapter posted! \"{bookTitle}\" — Ch.{chapterNumber}: {chapterTitle}. Unlocks for you in 24 hrs.",
                        readLink);
                    break;

                // ── Free ──────────────────────────────────────────────────
                // No notification. 24-hr gate enforced at read time.
                // They can still read — just silently, after 24 hrs.
                default:
                    break;
            }
        }
    }
}