using ReadEase.Enums;
using ReadEase.Models;
using ReadEase.Repository.Interfaces;
using ReadEase.Services.Interfaces;

namespace ReadEase.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repo;
    private readonly IUserRepository         _userRepo;
    private readonly IFavoriteRepository     _favRepo;
    private readonly INotificationService    _notifService;
    private readonly IAuditService           _auditService;

    public SubscriptionService(
        ISubscriptionRepository repo,
        IUserRepository         userRepo,
        IFavoriteRepository     favRepo,
        INotificationService    notifService,
        IAuditService           auditService)
    {
        _repo         = repo;
        _userRepo     = userRepo;
        _favRepo      = favRepo;
        _notifService = notifService;
        _auditService = auditService;
    }

    // ── GetStatusAsync ────────────────────────────────────────────────────
    public async Task<SubscriptionRequest?> GetStatusAsync(Guid userId)
        => await _repo.GetLatestByUserAsync(userId);

    // ── GetCurrentTierAsync ───────────────────────────────────────────────
    public async Task<SubscriptionTier> GetCurrentTierAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        return user?.SubscriptionTier ?? SubscriptionTier.Free;
    }

    // ── SubmitRequestAsync ────────────────────────────────────────────────
    public async Task SubmitRequestAsync(Guid userId, string? receiptPath, SubscriptionTier requestedTier)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.SubscriptionTier == requestedTier)
            throw new InvalidOperationException("You are already subscribed to that tier.");

        if (user.SubscriptionTier == SubscriptionTier.PremiumPlus)
            throw new InvalidOperationException("You are already on the highest tier (Premium++).");

        if (user.SubscriptionTier == SubscriptionTier.Premium
            && requestedTier != SubscriptionTier.PremiumPlus)
            throw new InvalidOperationException(
                "You are already subscribed to Premium. Upgrade to Premium++ instead.");

        var existing = await _repo.GetLatestByUserAsync(userId);
        if (existing?.Status == SubscriptionStatus.Pending)
            throw new InvalidOperationException("You already have a pending subscription request.");

        if (requestedTier == SubscriptionTier.Free)
            throw new InvalidOperationException("You cannot request a Free tier subscription.");

        await _repo.CreateAsync(new SubscriptionRequest
        {
            UserId        = userId,
            ReceiptPhoto  = receiptPath,
            Status        = SubscriptionStatus.Pending,
            SubmittedAt   = DateTime.UtcNow,
            RequestedTier = requestedTier
        });

        await _auditService.LogAsync(userId, "User",
            "SubscriptionRequested", $"UserId:{userId} Tier:{requestedTier}");
    }

    // ── GetAllRequestsAsync ───────────────────────────────────────────────
    public async Task<List<SubscriptionRequest>> GetAllRequestsAsync()
        => await _repo.GetAllWithUserAsync();

    // ── ApproveAsync ──────────────────────────────────────────────────────
    public async Task ApproveAsync(Guid requestId)
    {
        var all = await _repo.GetAllWithUserAsync();
        var req = all.FirstOrDefault(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Request not found.");

        req.Status     = SubscriptionStatus.Approved;
        req.ReviewedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(req);

        var user = await _userRepo.GetByIdAsync(req.UserId)
            ?? throw new InvalidOperationException("User not found.");

        user.SubscriptionTier = req.RequestedTier;
        await _userRepo.UpdateAsync(user);

        var tierLabel = req.RequestedTier == SubscriptionTier.PremiumPlus
            ? "Premium++ (PremiumPlus)"
            : "Premium";

        await _notifService.SendAsync(req.UserId,
            $"🎉 Your {tierLabel} subscription has been approved! Enjoy full access.",
            "/Mobile/Subscribe");

        await _auditService.LogAsync(null, "Admin",
            "SubscriptionApproved", $"RequestId:{requestId} Tier:{req.RequestedTier}");
    }

    // ── RejectAsync ───────────────────────────────────────────────────────
    public async Task RejectAsync(Guid requestId, string reason)
    {
        var all = await _repo.GetAllWithUserAsync();
        var req = all.FirstOrDefault(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Request not found.");

        req.Status     = SubscriptionStatus.Rejected;
        req.Reason     = reason;
        req.ReviewedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(req);

        await _notifService.SendAsync(req.UserId,
            $"❌ Your subscription request was rejected. Reason: {reason}",
            "/Mobile/Subscribe");

        await _auditService.LogAsync(null, "Admin",
            "SubscriptionRejected", $"RequestId:{requestId}");
    }

    // ── GetBookFollowersAsync ─────────────────────────────────────────────
    /// <summary>
    /// Returns all users who favourited the given book + their current tier.
    /// Used by ChapterReleaseJob for tier-aware notification fan-out.
    /// </summary>
    public async Task<List<BookFollowerDto>> GetBookFollowersAsync(Guid bookId)
    {
        // IFavoriteRepository.GetFanUsersForBookAsync returns List<User>
        // who have favourited this specific book.
        var users = await _favRepo.GetFanUsersForBookAsync(bookId);

        return users
            .Select(u => new BookFollowerDto(u.Id, u.SubscriptionTier))
            .ToList();
    }
}