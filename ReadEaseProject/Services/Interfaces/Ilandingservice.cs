using ReadEase.ViewModels.Landing;

namespace ReadEase.Services.Interfaces;

public interface ILandingService
{
    Task<LandingViewModel> GetLandingDataAsync();

    // ── Guest book preview ────────────────────────────────────────────────
    Task<GuestBookPreviewViewModel?> GetBookPreviewAsync(Guid bookId);
}