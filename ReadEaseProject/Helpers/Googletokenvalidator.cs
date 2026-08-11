using Google.Apis.Auth;

namespace ReadEase.Helpers;

public static class GoogleTokenValidator
{
    public static async Task<GoogleJsonWebSignature.Payload> ValidateIdTokenAsync(
        string idToken,
        string clientId)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        };

        return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
    }
}