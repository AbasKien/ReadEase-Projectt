using ReadEase.Enums;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Author;

namespace ReadEase.Services;

public class AuthorService : IAuthorService
{
    private readonly IBookRepository _bookRepo;
    private readonly IBookReadRepository _readRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationService _notifService;

    public AuthorService(
        IBookRepository bookRepo,
        IBookReadRepository readRepo,
        IUserRepository userRepo,
        INotificationService notifService)
    {
        _bookRepo = bookRepo;
        _readRepo = readRepo;
        _userRepo = userRepo;
        _notifService = notifService;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────
    public async Task<AuthorDashboardViewModel> GetDashboardAsync(
        Guid userId, string userName, string userEmail)
    {
        var books = await _bookRepo.GetByAuthorUserIdAsync(userId);
        var ids = books.Where(b => !b.IsDeleted).Select(b => b.Id).ToList();
        var reads = await _readRepo.GetReadCountsBatchAsync(ids);

        var items = MapToItems(books, reads);

        return new AuthorDashboardViewModel
        {
            Name = userName,
            Email = userEmail,
            TotalBooks = items.Count(b => !b.IsDeleted && b.Status == BookStatus.Active),
            TotalReads = items.Where(b => !b.IsDeleted).Sum(b => b.ReadCount),
            ArchivedCount = items.Count(b => b.IsDeleted),
            Books = items
        };
    }

    // ── My Books ──────────────────────────────────────────────────────────
    public async Task<ManageBooksViewModel> GetMyBooksAsync(Guid userId)
    {
        var books = await _bookRepo.GetByAuthorUserIdAsync(userId);
        var ids = books.Select(b => b.Id).ToList();
        var reads = await _readRepo.GetReadCountsBatchAsync(ids);
        var items = MapToItems(books, reads);

        return new ManageBooksViewModel
        {
            Books = items,
            TotalBooks = items.Count(b => !b.IsDeleted && b.Status == BookStatus.Active),
            TotalReads = items.Sum(b => b.ReadCount)
        };
    }

    // ── Archive ───────────────────────────────────────────────────────────
    public async Task<List<ManageBookItem>> GetArchiveAsync(Guid userId)
    {
        var books = await _bookRepo.GetDeletedByAuthorAsync(userId);
        var ids = books.Select(b => b.Id).ToList();
        var reads = await _readRepo.GetReadCountsBatchAsync(ids);
        return MapToItems(books, reads);
    }

    // ── Get book for edit (ownership-checked) ─────────────────────────────
    public async Task<PublishBookViewModel?> GetBookForEditAsync(Guid bookId, Guid userId)
    {
        var book = await _bookRepo.GetAnyByIdAsync(bookId);
        if (book is null || book.PublishedByUserId != userId) return null;

        return new PublishBookViewModel
        {
            Id = book.Id,
            Title = book.Title,
            Category = book.Category,
            Description = book.Description,
            IsFree = book.IsFree,
            ExistingCoverImage = book.CoverImage
        };
    }

    // ── Publish new book ──────────────────────────────────────────────────
    public async Task<(bool IsPending, bool WasPromoted)> PublishBookAsync(
        Guid userId, string authorName, PublishBookViewModel vm, string webRootPath)
    {
        var coverPath = await SaveCoverAsync(vm.CoverImageFile, webRootPath);
        var isPremium = !vm.IsFree;
        var status = isPremium ? BookStatus.PendingApproval : BookStatus.Active;

        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = vm.Title.Trim(),
            Category = vm.Category.Trim(),
            Description = vm.Description?.Trim(),
            Content = string.Empty,
            IsFree = vm.IsFree,
            CoverImage = coverPath,
            PublishedByUserId = userId,
            Author = authorName,
            PublishedAt = DateTime.UtcNow,
            Status = status
        };

        await _bookRepo.CreateAsync(book);

        var user = await _userRepo.GetByIdAsync(userId);
        bool promoted = false;
        if (user is not null && user.Role == UserRole.User)
        {
            user.Role = UserRole.Author;
            await _userRepo.UpdateAsync(user);
            promoted = true;
        }

        if (isPremium)
        {
            await _notifService.SendAsync(userId,
                $"📋 Your premium book \"{book.Title}\" has been submitted for admin approval.",
                "/Author/MyBooks");

            var admins = await _userRepo.GetAdminUsersAsync();
            foreach (var admin in admins)
            {
                await _notifService.SendAsync(
                    admin.Id,
                    $"📋 New premium approval request: \"{book.Title}\" by {authorName}",
                    "/Admin/PendingBooks");
            }
        }
        else
        {
            await _notifService.SendAsync(userId,
                $"🎉 Your book \"{book.Title}\" is now live in the library!",
                $"/Library/Detail/{book.Id}");
        }

        return (isPremium, promoted);
    }

    // ── Edit existing book ────────────────────────────────────────────────
    public async Task<bool> EditBookAsync(Guid userId, PublishBookViewModel vm, string webRootPath)
    {
        if (!vm.Id.HasValue) throw new InvalidOperationException("Book ID is required for edit.");

        var book = await _bookRepo.GetAnyByIdAsync(vm.Id.Value)
            ?? throw new InvalidOperationException("Book not found.");

        if (book.PublishedByUserId != userId)
            throw new UnauthorizedAccessException();

        bool wasFreePreviously = book.IsFree;
        bool wantsToGoPremium = !vm.IsFree;
        bool isPendingApproval = wasFreePreviously && wantsToGoPremium;

        book.Title = vm.Title.Trim();
        book.Description = vm.Description?.Trim();
        book.Category = vm.Category;
        book.IsFree = vm.IsFree;

        if (vm.CoverImageFile is { Length: > 0 })
            book.CoverImage = await SaveCoverAsync(vm.CoverImageFile, webRootPath);

        if (isPendingApproval)
        {
            book.Status = BookStatus.PendingApproval;
            book.IsFree = false;

            await _bookRepo.UpdateAsync(book);

            await _notifService.SendAsync(userId,
                $"📋 Your book \"{book.Title}\" has been submitted for admin approval to become Premium.",
                "/Author/MyBooks");

            var admins = await _userRepo.GetAdminUsersAsync();
            foreach (var admin in admins)
            {
                await _notifService.SendAsync(
                    admin.Id,
                    $"📋 Premium upgrade request: \"{book.Title}\" by {book.Author}",
                    "/Admin/PendingBooks");
            }

            return true;
        }

        await _bookRepo.UpdateAsync(book);
        return false;
    }

    // ── Soft-delete (archive) ─────────────────────────────────────────────
    public async Task ArchiveBookAsync(Guid bookId, Guid userId)
    {
        var book = await _bookRepo.GetAnyByIdAsync(bookId)
            ?? throw new InvalidOperationException("Book not found.");
        if (book.PublishedByUserId != userId)
            throw new UnauthorizedAccessException("You do not own this book.");
        book.IsDeleted = true;
        await _bookRepo.UpdateAsync(book);
    }

    // ── Restore from archive ──────────────────────────────────────────────
    public async Task RestoreBookAsync(Guid bookId, Guid userId)
    {
        var book = await _bookRepo.GetDeletedBookByIdAsync(bookId)
            ?? throw new InvalidOperationException("Book not found.");
        if (book.PublishedByUserId != userId)
            throw new UnauthorizedAccessException("You do not own this book.");
        book.IsDeleted = false;
        await _bookRepo.UpdateAsync(book);
    }

    // ── Permanent delete ──────────────────────────────────────────────────
    public async Task PermanentDeleteAsync(Guid bookId, Guid userId)
    {
        var book = await _bookRepo.GetDeletedBookByIdAsync(bookId)
            ?? throw new InvalidOperationException("Book not found.");
        if (book.PublishedByUserId != userId)
            throw new UnauthorizedAccessException("You do not own this book.");
        await _bookRepo.HardDeleteAsync(bookId);
    }

    // ── Toggle Story Complete ─────────────────────────────────────────────
    /// <summary>
    /// Toggles the author's IsStoryComplete flag on the book.
    /// When marking COMPLETE → auto-completes all readers who have already
    /// finished every published chapter (moves them from Ongoing → Completed).
    /// When un-marking (back to Ongoing) → the flag is cleared but reader
    /// records are NOT rolled back; they stay completed.
    /// </summary>
    public async Task<bool> ToggleStoryCompleteAsync(Guid bookId, Guid userId)
    {
        var book = await _bookRepo.GetAnyByIdAsync(bookId)
            ?? throw new InvalidOperationException("Book not found.");

        if (book.PublishedByUserId != userId)
            throw new UnauthorizedAccessException("You do not own this book.");

        book.IsStoryComplete = !book.IsStoryComplete;
        await _bookRepo.UpdateAsync(book);

        // When author marks story COMPLETE, auto-complete all readers
        // who have already read every chapter.
        if (book.IsStoryComplete)
            await _readRepo.AutoCompleteFinishedReadersAsync(bookId);

        return book.IsStoryComplete;
    }

    // ── Private helpers ───────────────────────────────────────────────────
    private List<ManageBookItem> MapToItems(
        List<Book> books, Dictionary<Guid, int> reads)
        => books.Select(b => new ManageBookItem
        {
            Id = b.Id,
            Title = b.Title,
            Category = b.Category,
            CoverImage = b.CoverImage,
            IsFree = b.IsFree,
            IsDeleted = b.IsDeleted,
            PublishedAt = b.PublishedAt,
            ReadCount = reads.GetValueOrDefault(b.Id, 0),
            Status = b.Status,
            RejectionReason = b.RejectionReason
        }).ToList();

    private static async Task<string?> SaveCoverAsync(IFormFile? file, string webRootPath)
    {
        if (file is not { Length: > 0 }) return null;
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext)) return null;
        var uploads = Path.Combine(webRootPath, "uploads", "covers");
        Directory.CreateDirectory(uploads);
        var fileName = $"{Guid.NewGuid()}{ext}";
        await using var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/covers/{fileName}";
    }
}