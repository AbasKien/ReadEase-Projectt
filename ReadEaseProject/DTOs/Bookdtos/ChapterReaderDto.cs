namespace ReadEase.DTOs.Bookdtos;

public class ChapterReaderDto
{
    public int CurrentChapter { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// UTC time the current chapter unlocks for Premium readers.
    /// Used in LibraryController.Read to apply the premium tier gate.
    /// </summary>
    public DateTime? PremiumUnlocksAt { get; set; }

    public List<ChapterListDto> AllChapters { get; set; } = [];
}