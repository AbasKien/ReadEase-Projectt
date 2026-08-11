// ─────────────────────────────────────────────────────────────────────────────
// ViewModels/Library/ChapterNavItem.cs
// Add these properties to your existing ChapterNavItem class
// ─────────────────────────────────────────────────────────────────────────────

namespace ReadEase.ViewModels.Library;

public class ChapterNavItem
{
    public Guid Id { get; set; }
    public int ChapterNumber { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>True once the chapter is publicly live for free readers.</summary>
    public bool IsVisible { get; set; }

    public string Content { get; set; } = string.Empty; // ✅ NEW - include content for mobile chapter list


    /// <summary>
    /// UTC time this chapter becomes visible to FREE readers (= ScheduledAt on the entity).
    /// Null for immediately-published chapters.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// UTC time this chapter unlocks for PREMIUM (non-Plus) readers.
    /// = ScheduledAt - 24 hours. Null for immediately-published chapters.
    /// </summary>
    public DateTime? PremiumUnlocksAt { get; set; }
}