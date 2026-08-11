using ReadEase.DTOs.Auth;
using ReadEase.DTOs.Landing;

namespace ReadEase.ViewModels.Landing;

public class LandingViewModel
{
    public List<LandingBookDto> TopRated { get; set; } = new();
    public List<LandingBookDto> MostViewed { get; set; } = new();
    public List<LandingBookDto> AllBooks { get; set; } = new();   // ← NEW: used for genre panel
    public List<LandingGenreDto> Genres { get; set; } = new();
    public int TotalBooks { get; set; }
    public int TotalGenres { get; set; }
    public double SiteAvgRating { get; set; }
}