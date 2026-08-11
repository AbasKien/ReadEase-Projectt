using ReadEase.Enums;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;
using ReadEase.ViewModels.Admin;

namespace ReadEase.Services;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepo;
    private readonly IBookRepository _bookRepo;
    private readonly INotificationService _notifService;
    private readonly IAuditService _auditService;
    private readonly ISubscriptionService _subService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IUserRepository userRepo,
        IBookRepository bookRepo,
        INotificationService notifService,
        IAuditService auditService,
        ISubscriptionService subService,
        ILogger<AdminService> logger)
    {
        _userRepo = userRepo;
        _bookRepo = bookRepo;
        _notifService = notifService;
        _auditService = auditService;
        _subService = subService;
        _logger = logger;
    }

    // ── Dashboard stats ───────────────────────────────────────────────────
    public async Task<AdminDashboardViewModel> GetDashboardStatsAsync()
    {
        var users = await _userRepo.GetAllAsync();
        var pendingBooks = await _bookRepo.GetPendingAsync();
        var allSubs = await _subService.GetAllRequestsAsync();
        var pendingSubs = allSubs.Count(s => s.Status == SubscriptionStatus.Pending);

        return new AdminDashboardViewModel
        {
            TotalUsers = users.Count(u => u.Role != UserRole.Admin),
            TotalAuthors = users.Count(u => u.Role == UserRole.Author),
            PendingPremiumBooks = pendingBooks.Count,
            PendingSubscriptions = pendingSubs,
            ActiveUsers = users.Count(u => u.IsActive && u.Role != UserRole.Admin),
            InactiveUsers = users.Count(u => !u.IsActive)
        };
    }

    // ── Pending premium books ─────────────────────────────────────────────
    public async Task<List<PendingBookViewModel>> GetPendingBooksAsync()
    {
        var books = await _bookRepo.GetPendingAsync();
        var users = await _userRepo.GetAllAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.Email);

        return books.Select(b => new PendingBookViewModel
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            Category = b.Category,
            CoverImage = b.CoverImage,
            Description = b.Description,
            SubmittedAt = b.PublishedAt,
            PublishedByUserId = b.PublishedByUserId,
            PublishedByEmail = userMap.GetValueOrDefault(b.PublishedByUserId, "Unknown")
        }).ToList();
    }

    public async Task ApprovePremiumBookAsync(Guid bookId, Guid adminId)
    {
        var book = await _bookRepo.GetAnyByIdAsync(bookId)
            ?? throw new InvalidOperationException("Book not found.");

        if (book.Status != BookStatus.PendingApproval)
            throw new InvalidOperationException("Book is not pending approval.");

        book.Status = BookStatus.Active;
        book.RejectionReason = null;
        await _bookRepo.UpdateAsync(book);

        await _notifService.SendAsync(book.PublishedByUserId,
            $"✅ Your premium book \"{book.Title}\" has been approved and is now live!",
            $"/Library/Detail/{book.Id}");

        await _auditService.LogAsync(adminId, "Admin",
            "ApprovePremiumBook", $"BookId:{bookId} Title:{book.Title}");

        _logger.LogInformation("Premium book {BookId} approved by admin {AdminId}",
            bookId, adminId);
    }

    public async Task RejectPremiumBookAsync(Guid bookId, string reason, Guid adminId)
    {
        var book = await _bookRepo.GetAnyByIdAsync(bookId)
            ?? throw new InvalidOperationException("Book not found.");

        if (book.Status != BookStatus.PendingApproval)
            throw new InvalidOperationException("Book is not pending approval.");

        book.Status = BookStatus.Rejected;
        book.RejectionReason = reason;
        await _bookRepo.UpdateAsync(book);

        await _notifService.SendAsync(book.PublishedByUserId,
            $"❌ Your premium book \"{book.Title}\" was not approved. Reason: {reason}",
            "/Author/MyBooks");

        await _auditService.LogAsync(adminId, "Admin",
            "RejectPremiumBook", $"BookId:{bookId} Reason:{reason}");

        _logger.LogInformation("Premium book {BookId} rejected by admin {AdminId}",
            bookId, adminId);
    }

    // ── User management ───────────────────────────────────────────────────
    public async Task<List<UserManagementViewModel>> GetAllUsersAsync()
    {
        var users = await _userRepo.GetAllAsync();
        return users
            .Where(u => u.Role != UserRole.Admin)
            .Select(u => new UserManagementViewModel
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role.ToString(),
                Provider = u.Provider,
                IsActive = u.IsActive,
                IsEmailConfirmed = u.IsEmailConfirmed,
                CreatedAt = u.CreatedAt
            }).ToList();
    }

    public async Task ToggleUserActiveAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");
        if (user.Role == UserRole.Admin)
            throw new InvalidOperationException("Cannot deactivate admin accounts.");
        user.IsActive = !user.IsActive;
        await _userRepo.UpdateAsync(user);
    }
}