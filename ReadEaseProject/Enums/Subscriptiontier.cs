namespace ReadEase.Enums
{
    /// <summary>
    /// Represents the active subscription tier of a user.
    /// Free     → read free books only, no notifications.
    /// Premium  → access premium books.
    /// PremiumPlus → notifications, book downloads, and AI voice reader.
    /// </summary>
    public enum SubscriptionTier
    {
        Free = 10,
        Premium = 11,
        PremiumPlus = 12
    }
}