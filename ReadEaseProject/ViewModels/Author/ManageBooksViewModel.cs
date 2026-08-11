namespace ReadEase.ViewModels.Author
{
    public class ManageBooksViewModel
    {
        public List<ManageBookItem> Books { get; set; } = new();
        public int TotalBooks { get; set; }
        public int TotalReads { get; set; }
    }
}
