namespace ReadEase.DTOs.Bookdtos
{
    public class ManageChaptersDto
    {
        public Guid BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public bool IsStoryComplete { get; set; }
        public List<ChapterListDto> Chapters { get; set; } = new();
    }
}
