namespace ReadEase.ViewModels.Library;

public class ReadingProgressViewModel
{
    public List<ReadingBookCardViewModel> Books { get; set; } = new();
    public int OngoingCount { get; set; }
    public int CompletedCount { get; set; }
    public bool IsSubscribed { get; set; }
}

public class ReadingBookCardViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public bool IsFree { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime ReadAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int LastChapterRead { get; set; }
    public int TotalChapters { get; set; }

    public int ProgressPercent =>
        IsCompleted ? 100 :
        TotalChapters > 0 ? Math.Clamp(LastChapterRead * 100 / TotalChapters, 1, 99) : 0;
}