namespace ReadEase.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }

    // Populated only on Register — the raw token the controller uses to build
    // the confirmation link. Never sent to the mobile client in the JSON response.
    [System.Text.Json.Serialization.JsonIgnore]
    public string? VerificationToken { get; set; }
}