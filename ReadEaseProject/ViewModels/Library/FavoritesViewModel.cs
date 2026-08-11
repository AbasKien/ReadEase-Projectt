namespace ReadEase.ViewModels.Library
{
    public class FavoritesViewModel
    {
        public List<BookCardViewModel> Books { get; set; } = new();
        public bool IsSubscribed { get; set; }
    }
}
