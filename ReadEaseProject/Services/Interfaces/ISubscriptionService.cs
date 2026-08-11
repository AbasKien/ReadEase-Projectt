using ReadEase.Enums;
using ReadEase.Models;

namespace ReadEase.Services.Interfaces;

/// <summary>DTO returned by GetBookFollowersAsync — userId + their current tier.</summary>
public sealed record BookFollowerDto(Guid UserId, SubscriptionTier Tier);

public interface ISubscriptionService
{
    Task<SubscriptionRequest?> GetStatusAsync(Guid userId);

    /// <summary>Returns the user's actual current SubscriptionTier from the database.</summary>
    Task<SubscriptionTier> GetCurrentTierAsync(Guid userId);

    /// <summary>
    /// Submit a new subscription request for the given tier.
    /// Free → Premium or PremiumPlus : allowed.
    /// Premium → PremiumPlus         : allowed (upgrade).
    /// PremiumPlus → anything        : blocked (already on highest tier).
    /// </summary>
    Task SubmitRequestAsync(Guid userId, string? receiptPath, SubscriptionTier requestedTier);

    Task<List<SubscriptionRequest>> GetAllRequestsAsync();

    /// <summary>Approve the request — sets User.SubscriptionTier to the RequestedTier.</summary>
    Task ApproveAsync(Guid requestId);

    Task RejectAsync(Guid requestId, string reason);

    /// <summary>
    /// Returns all users who have favourited the given book, along with their current tier.
    /// Used by ChapterReleaseJob to fan-out tier-aware notifications.
    /// </summary>
    Task<List<BookFollowerDto>> GetBookFollowersAsync(Guid bookId);
}