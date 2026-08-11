using Microsoft.AspNetCore.Identity;
using ReadEase.Models;

namespace ReadEase.Helpers;

public class PasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(User user, string password)
        => _hasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string password, string storedHash)
    {
        var result = _hasher.VerifyHashedPassword(user, storedHash, password);
        return result != PasswordVerificationResult.Failed;
    }
}