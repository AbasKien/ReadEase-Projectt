using ReadEase.DTOs.Bookdtos;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Author;

namespace ReadEase.Services;

public class ChapterService : IChapterService
{
    private readonly IBookChapterRepository _chapterRepo;
    private readonly IBookRepository _bookRepo;
    private readonly IFavoriteRepository _favRepo;
    private readonly INotificationService _notify;

    public ChapterService(
        IBookChapterRepository chapterRepo,
        IBookRepository bookRepo,
        IFavoriteRepository favRepo,
        INotificationService notify)
    {
        _chapterRepo = chapterRepo;
        _bookRepo = bookRepo;
        _favRepo = favRepo;
        _notify = notify;
    }

    // ── Author: manage view ───────────────────────────────────────────────
    public async Task<ManageChaptersDto> GetManageViewAsync(Guid bookId, Guid authorUserId)
    {
        var book = await _bookRepo.GetByIdAsync(bookId)
            ?? throw new InvalidOperationException("Book not found.");

        if (book.PublishedByUserId != authorUserId)
            throw new UnauthorizedAccessException();

        var chapters = await _chapterRepo.GetByBookAsync(bookId);

        return new ManageChaptersDto
        {
            BookId = book.Id,
            BookTitle = book.Title,
            CoverImage = book.CoverImage,
            IsStoryComplete = book.IsStoryComplete,
            Chapters = chapters.Select(c => new ChapterListDto
            {
                Id = c.Id,
                ChapterNumber = c.ChapterNumber,
                Title = c.Title,
                PublishedAt = c.PublishedAt,
                ScheduledAt = c.ScheduledAt,
                PremiumUnlocksAt = c.PremiumUnlocksAt,
                IsVisible = c.IsVisible
            }).ToList()
        };
    }

    // ── Author: add-chapter form data ─────────────────────────────────────
    public async Task<PublishChapterViewModel> GetAddFormAsync(Guid bookId, Guid authorUserId)
    {
        var book = await _bookRepo.GetByIdAsync(bookId)
            ?? throw new InvalidOperationException("Book not found.");

        if (book.PublishedByUserId != authorUserId)
            throw new UnauthorizedAccessException();

        return new PublishChapterViewModel
        {
            BookId = bookId,
            BookTitle = book.Title,
            ChapterNumber = await _chapterRepo.NextChapterNumberAsync(bookId)
        };
    }

    // ── Author: edit-chapter form data ────────────────────────────────────
    public async Task<EditChapterViewModel?> GetEditFormAsync(Guid chapterId, Guid authorUserId)
    {
        var chapter = await _chapterRepo.GetByIdAsync(chapterId);
        if (chapter is null) return null;

        var book = await _bookRepo.GetByIdAsync(chapter.BookId)
            ?? throw new InvalidOperationException("Book not found.");

        if (book.PublishedByUserId != authorUserId)
            throw new UnauthorizedAccessException();

        return new EditChapterViewModel
        {
            Id = chapter.Id,
            BookId = chapter.BookId,
            BookTitle = book.Title,
            ChapterNumber = chapter.ChapterNumber,
            Title = chapter.Title,
            Content = chapter.Content
        };
    }

    // ── Author: publish / schedule a new chapter ──────────────────────────
    /// <summary>
    /// Creates the chapter row.
    ///
    /// Scheduling rules:
    ///   ScheduledAt  = the FREE reader unlock time (e.g. March 30 08:00 UTC)
    ///   PremiumUnlocksAt = ScheduledAt - 24 hours  (e.g. March 29 08:00 UTC)
    ///
    /// If dto.ScheduledAt is null → publish immediately → IsVisible = true,
    ///   no delays for anyone, fans are notified now.
    ///
    /// If dto.ScheduledAt is set → IsVisible = false, Hangfire flips it at ScheduledAt.
    ///   Premium fans are notified at PremiumUnlocksAt (separate Hangfire job in controller).
    ///   Free fans are notified at ScheduledAt by ChapterReleaseJob.
    ///
    /// Returns (chapterNumber, chapterId).
    /// </summary>
    public async Task<(int ChapterNumber, Guid ChapterId)> PublishChapterAsync(
        PublishChapterDto dto, Guid authorUserId, string authorName)
    {
        var book = await _bookRepo.GetByIdAsync(dto.BookId)
            ?? throw new InvalidOperationException("Book not found.");

        if (book.PublishedByUserId != authorUserId)
            throw new UnauthorizedAccessException();

        var nextNum = await _chapterRepo.NextChapterNumberAsync(dto.BookId);
        var isScheduled = dto.ScheduledAt.HasValue && dto.ScheduledAt.Value > DateTime.UtcNow;
        var isVisible = !isScheduled;

        // PremiumUnlocksAt = free unlock time - 24 hours
        // Only set when chapter is scheduled; immediate publishes have no tier delay.
        DateTime? premiumUnlocksAt = isScheduled
            ? dto.ScheduledAt!.Value.ToUniversalTime().AddHours(-24)
            : null;

        var chapter = new BookChapter
        {
            Id = Guid.NewGuid(),
            BookId = dto.BookId,
            ChapterNumber = nextNum,
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            // PublishedAt = the moment the chapter goes live for free readers
            PublishedAt = isVisible ? DateTime.UtcNow : dto.ScheduledAt!.Value.ToUniversalTime(),
            ScheduledAt = isScheduled ? dto.ScheduledAt!.Value.ToUniversalTime() : null,
            PremiumUnlocksAt = premiumUnlocksAt,
            IsVisible = isVisible,
            HangfireJobId = null   // saved later via SaveJobIdAsync
        };

        await _chapterRepo.CreateAsync(chapter);

        // ── Fan-out notification for immediate publishes ───────────────────
        // Scheduled chapters: ChapterReleaseJob sends free-reader notifications at ScheduledAt,
        // and a separate Hangfire job (scheduled by AuthorController) notifies premium fans
        // at PremiumUnlocksAt.
        if (isVisible)
        {
            var fanIds = await _favRepo.GetFanUserIdsForAuthorAsync(authorUserId);
            if (fanIds.Count > 0)
            {
                var message = $"📖 New chapter in \"{book.Title}\" — Ch.{nextNum}: {chapter.Title}";
                var link = $"/Library/Read/{dto.BookId}?chapter={nextNum}";
                foreach (var uid in fanIds)
                    await _notify.SendAsync(uid, message, link);
            }
        }

        return (nextNum, chapter.Id);
    }

    // ── Hangfire: flip chapter visible at ScheduledAt ─────────────────────
    /// <summary>
    /// Called by ChapterReleaseJob at ScheduledAt (the FREE reader unlock time).
    /// Sets IsVisible = true so free readers can now access the chapter.
    /// </summary>
    public async Task PublishScheduledChapterAsync(Guid chapterId)
    {
        var chapter = await _chapterRepo.GetByIdAsync(chapterId)
            ?? throw new InvalidOperationException("Chapter not found.");

        chapter.IsVisible = true;
        chapter.PublishedAt = DateTime.UtcNow;
        await _chapterRepo.UpdateAsync(chapter);
    }

    // ── Save Hangfire job ID ──────────────────────────────────────────────
    public async Task SaveJobIdAsync(Guid chapterId, string hangfireJobId)
    {
        var chapter = await _chapterRepo.GetByIdAsync(chapterId)
            ?? throw new InvalidOperationException("Chapter not found.");

        chapter.HangfireJobId = hangfireJobId;
        await _chapterRepo.UpdateAsync(chapter);
    }

    // ── Author: edit existing chapter ─────────────────────────────────────
    public async Task EditChapterAsync(EditChapterDto dto, Guid authorUserId)
    {
        var chapter = await _chapterRepo.GetByIdAsync(dto.ChapterId)
            ?? throw new InvalidOperationException("Chapter not found.");

        var book = await _bookRepo.GetByIdAsync(chapter.BookId)
            ?? throw new InvalidOperationException("Book not found.");

        if (book.PublishedByUserId != authorUserId)
            throw new UnauthorizedAccessException();

        chapter.Title = dto.Title.Trim();
        chapter.Content = dto.Content.Trim();
        await _chapterRepo.UpdateAsync(chapter);
    }

    // ── Author: soft-delete chapter ───────────────────────────────────────
    public async Task DeleteChapterAsync(Guid chapterId, Guid authorUserId)
    {
        var chapter = await _chapterRepo.GetByIdAsync(chapterId)
            ?? throw new InvalidOperationException("Chapter not found.");

        var book = await _bookRepo.GetByIdAsync(chapter.BookId)
            ?? throw new InvalidOperationException("Book not found.");

        if (book.PublishedByUserId != authorUserId)
            throw new UnauthorizedAccessException();

        await _chapterRepo.DeleteAsync(chapterId);
    }

    // ── Reader: resolve chapter list for Detail page ─────────────────────
    /// <summary>
    /// Returns chapters visible to the user on the Detail / chapter-list page.
    ///
    /// Visibility rules:
    ///   Premium++ / Admin / Author → sees all IsVisible chapters + scheduled (not yet live) ones
    ///   Premium                    → sees IsVisible + scheduled chapters (but read-gate applies in Read action)
    ///   Free                       → sees IsVisible chapters only
    ///
    /// The actual read-gate (can they open the content?) is enforced in LibraryController.Read.
    /// Here we only control which chapters APPEAR in the list.
    /// </summary>
    public async Task<ChapterReaderDto?> GetReaderDataAsync(
        Guid bookId, int? requestedChapter,
        bool isPremium = false, bool isPremiumPlus = false, bool bypassGate = false)
    {
        var chapters = await _chapterRepo.GetByBookAsync(bookId);
        if (chapters.Count == 0) return null;

        // Chapters shown in the list (what appears in the chapter nav / Detail page):
        //   • bypassGate (admin/author) → everything, including drafts with no schedule
        //   • everyone else → IsVisible (live) + any chapter with a ScheduledAt date
        //     Free users see scheduled chapters too — they appear LOCKED with the release
        //     date shown. The read-gate in Detail.cshtml and LibraryController.Read
        //     prevents them from actually opening the content until the right time.
        // Declare now here so it's available to both listable and readable filters.
        var now = DateTime.UtcNow;

        List<BookChapter> listable;
        if (bypassGate)
            listable = chapters.ToList();
        else
            // A chapter is listable when:
            //   (a) IsVisible = true  — fully live, OR
            //   (b) ScheduledAt is set — show as locked with release date, OR
            //   (c) IsVisible = false but PublishedAt is in the past and no ScheduledAt
            //       — chapter was published immediately but IsVisible was never flipped
            //       (e.g. Hangfire failure or data issue). Treat it as live.
            listable = chapters.Where(c =>
                c.IsVisible ||
                c.ScheduledAt.HasValue ||
                (!c.IsVisible && !c.ScheduledAt.HasValue && c.PublishedAt <= now)
            ).ToList();

        if (listable.Count == 0) return null;

        // The chapter whose content we load for the reader view.
        // We build a "readable" list that respects each tier's actual access window:
        //   • bypassGate / PremiumPlus → all chapters immediately
        //   • Premium                  → IsVisible chapters + chapters where the premium
        //                               early-access window (PremiumUnlocksAt) has already
        //                               opened, even if Hangfire hasn't flipped IsVisible yet
        //   • Free                     → IsVisible chapters only
        List<BookChapter> readable;
        if (bypassGate || isPremiumPlus)
            readable = chapters.ToList();
        else if (isPremium)
            readable = chapters
                .Where(c => c.IsVisible ||
                            (c.PremiumUnlocksAt.HasValue && now >= c.PremiumUnlocksAt.Value) ||
                            (!c.IsVisible && !c.ScheduledAt.HasValue && c.PublishedAt <= now))
                .ToList();
        else
            // Free readers: IsVisible chapters + any chapter whose PublishedAt has passed
            // but IsVisible wasn't flipped yet (data/Hangfire issue).
            readable = chapters
                .Where(c => c.IsVisible ||
                            (!c.IsVisible && !c.ScheduledAt.HasValue && c.PublishedAt <= now))
                .ToList();

        BookChapter? target = null;
        if (requestedChapter.HasValue)
        {
            target = readable.FirstOrDefault(c => c.ChapterNumber == requestedChapter.Value)
                     ?? readable.FirstOrDefault();
        }
        else
        {
            target = readable.FirstOrDefault();
        }

        // For the gate check in LibraryController.Read we need the REQUESTED chapter's
        // lock timestamps, not target's. If target resolved to a different chapter (e.g.
        // because the requested one isn't readable yet), we'd return null/wrong timestamps
        // and the gate would pass silently. Grab the raw entity as a fallback.
        BookChapter? requestedEntity = requestedChapter.HasValue
            ? chapters.FirstOrDefault(c => c.ChapterNumber == requestedChapter.Value)
            : null;

        return new ChapterReaderDto
        {
            CurrentChapter = target?.ChapterNumber ?? 0,
            Content = target?.Content ?? string.Empty,
            PublishedAt = target?.PublishedAt
                          ?? requestedEntity?.PublishedAt
                          ?? DateTime.UtcNow,
            PremiumUnlocksAt = target?.PremiumUnlocksAt
                               ?? requestedEntity?.PremiumUnlocksAt,
            AllChapters = listable.Select(c => new ChapterListDto
            {
                Id = c.Id,
                ChapterNumber = c.ChapterNumber,
                Title = c.Title,
                Content = c.Content,
                PublishedAt = c.PublishedAt,
                ScheduledAt = c.ScheduledAt,
                PremiumUnlocksAt = c.PremiumUnlocksAt,
                IsVisible = c.IsVisible
            }).ToList()
        };
    }
}