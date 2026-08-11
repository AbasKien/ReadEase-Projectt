using ReadEase.Models;

namespace ReadEase.Services;

public interface ITokenService
{
    string GenerateJwtToken(User user);
    DateTime GetExpiration();
}