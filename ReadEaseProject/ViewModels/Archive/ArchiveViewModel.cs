namespace ReadEase.ViewModels.Archive
{
    public class ArchiveViewModel
    {
        public List<BookArchiveItem> DeletedBooks { get; set; } = new();
        public List<UserArchiveItem> InactiveUsers { get; set; } = new();
    }

}
