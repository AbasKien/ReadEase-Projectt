using ReadEase.ViewModels.Profile;

namespace ReadEase.Services.Interfaces;

public interface IProfileService
{
    Task<ProfileViewModel?> GetProfileAsync(Guid userId);
    Task UpdateProfileAsync(Guid userId, EditProfileViewModel vm);
    Task ChangePasswordAsync(Guid userId, ChangePasswordViewModel vm);
    Task ApplyAsAuthorAsync(Guid userId, ApplyAsAuthorViewModel vm);
    /// <summary>Populates TotalReads, TotalFavorites, TotalBooks on an existing ProfileViewModel.</summary>
    Task PopulateStatsAsync(Guid userId, ProfileViewModel profile);
}