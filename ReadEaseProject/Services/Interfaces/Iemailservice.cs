namespace ReadEase.Services;

public interface IEmailService
{
    Task SendConfirmationEmailAsync(string toEmail, string userName, string confirmationLink);
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink);
    Task SendPasswordResetOtpEmailAsync(string toEmail, string userName, string otp);
}