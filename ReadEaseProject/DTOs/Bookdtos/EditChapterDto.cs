namespace ReadEase.DTOs.Bookdtos;

public class EditChapterDto
{
    public Guid ChapterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}