using ReadEase.Enums;
using System.ComponentModel.DataAnnotations;

namespace ReadEase.ViewModels.Library;

public class LibraryViewModel
{
    public List<BookCardViewModel> Books { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string? SelectedCategory { get; set; }
    public bool IsSubscribed { get; set; }
    public bool IsAdmin { get; set; }

    // ── Reading progress counts (for navbar badges) ───────────────────────
    public int OngoingCount { get; set; }
    public int CompletedCount { get; set; }

    // ── Featured sections ─────────────────────────────────────────────────
    public List<BookCardViewModel> TopPicks { get; set; } = new();
    public List<BookCardViewModel> NewReleases { get; set; } = new();
    public List<BookCardViewModel> RecommendedForYou { get; set; } = new();
}