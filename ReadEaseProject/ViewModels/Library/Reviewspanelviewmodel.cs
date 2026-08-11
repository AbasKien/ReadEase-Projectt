using ReadEase.DTOs.Book;

namespace ReadEase.ViewModels.Library.Partials
{
    public class ReviewsPanelViewModel
    {
        public bool IsBookAuthor { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public List<ReviewDto> Reviews { get; set; } = [];
    }
}