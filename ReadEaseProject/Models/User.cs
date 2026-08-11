using ReadEase.Enums;
namespace ReadEase.Models;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Provider { get; set; } = "Local";
    public string? ExternalId { get; set; }

    // ── Email Confirmation ────────────────────────────────────────────────
    public bool IsEmailConfirmed { get; set; } = false;
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpiry { get; set; }

    // ── Password Reset ────────────────────────────────────────────────────
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // ── Profile ───────────────────────────────────────────────────────────
    public string? ProfilePhoto { get; set; }
    public string? CoverPhoto { get; set; }
    public bool IsActive { get; set; } = true;

    // ── Subscription ──────────────────────────────────────────────────────
    /// <summary>
    /// Replaces the old IsSubscribed bool.
    /// Free = 0 (default), Premium = 1, PremiumPlus = 2.
    /// Use helper properties below for clean feature-gate checks.
    /// </summary>
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;

    // ── Convenience helpers (not mapped to DB columns) ────────────────────
    /// <summary>True for Premium and PremiumPlus users.</summary>
    public bool IsSubscribed => SubscriptionTier >= SubscriptionTier.Premium;

    /// <summary>True only for PremiumPlus users.</summary>
    public bool IsPremiumPlus => SubscriptionTier == SubscriptionTier.PremiumPlus;
}