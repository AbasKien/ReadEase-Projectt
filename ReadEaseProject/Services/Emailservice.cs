using System.Net;
using System.Net.Mail;

namespace ReadEase.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ── OTP Verification Email ────────────────────────────────────────────
    public async Task SendConfirmationEmailAsync(string toEmail, string userName, string otp)
    {
        // Safety guard — OTP must be exactly 6 digits
        var safeOtp = otp?.Trim() ?? "";
        if (safeOtp.Length > 6) safeOtp = safeOtp.Substring(0, 6);

        var subject = "Your ReadEase verification code";
        var body = $@"
        <div style='font-family:Georgia,serif;max-width:480px;margin:2rem auto;
                    background:#FFFCF7;border-radius:20px;padding:2.5rem 2rem;
                    border:2px solid #E4D8C4;box-shadow:0 8px 32px rgba(44,26,14,.1);'>

            <!-- Logo -->
            <div style='text-align:center;margin-bottom:1.5rem;'>
                <table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center'>
                    <tr>
                        <td style='width:75px;height:75px;border-radius:50%;border:3px dashed #C8860A;text-align:center;vertical-align:middle;background:#FFFCF7;'>
                            <div style='font-size:35px;line-height:75px;'>📖</div>
                        </td>
                    </tr>
                </table>
                <div style='font-size:2.1rem;font-weight:800;color:#C8860A;letter-spacing:-.02em;margin-top:1rem;'>
                    ReadEase
                </div>
                <div style='font-size:.8rem;color:#A08060;margin-top:.2rem;letter-spacing:.15em;text-transform:uppercase;'>
                    Your Reading Companion
                </div>
            </div>

            <!-- Gold Divider -->
            <div style='width:80px;height:4px;background:linear-gradient(90deg,#C8860A 0%,#E4D8C4 50%,#C8860A 100%);margin:1.5rem auto;border-radius:2px;'></div>

            <!-- Title -->
            <h2 style='color:#2C1A0E;text-align:center;font-size:1.5rem;font-weight:700;
                       margin:1.5rem 0 .75rem 0;font-family:Georgia,serif;letter-spacing:-.01em;'>
                Verify Your Email
            </h2>

            <!-- Message -->
            <p style='color:#A08060;text-align:center;font-size:1rem;line-height:1.7;
                      margin-bottom:2rem;padding:0 .5rem;'>
                Hi <strong style='color:#C8860A;'>{userName}</strong>, here's your verification code
            </p>

            <!-- OTP Container -->
            <div style='background:linear-gradient(135deg,#F5EFE3 0%,#FAF6EF 100%);
                        border:2px solid #C8860A;
                        border-radius:16px;
                        padding:1.75rem 1.5rem;
                        text-align:center;
                        margin:0 auto 1.75rem auto;
                        max-width:320px;
                        box-shadow:0 4px 16px rgba(200,134,10,.15);'>
                <div style='display:flex;justify-content:center;gap:8px;flex-wrap:nowrap;'>
                    {string.Join("", safeOtp.Select(c => $@"
                    <span style='display:inline-flex;align-items:center;justify-content:center;
                                 width:50px;height:60px;
                                 background:#FFFCF7;
                                 border:2.5px solid #C8860A;
                                 border-radius:14px;
                                 font-size:2rem;
                                 font-weight:900;
                                 color:#C8860A;
                                 font-family:''Courier New'',monospace;
                                 box-shadow:0 4px 12px rgba(200,134,10,.2);'>{c}</span>"))}
                </div>
            </div>

            <!-- Timer Badge -->
            <div style='text-align:center;margin-bottom:1.5rem;'>
                <span style='display:inline-block;background:#F5EFE3;border:1px solid #E4D8C4;border-radius:25px;
                            padding:.5rem 1.25rem;font-size:.9rem;color:#A08060;'>
                    ⏱️ Expires in <strong style='color:#C8860A;font-weight:800;'>10 minutes</strong>
                </span>
            </div>

            <!-- Security Note -->
            <div style='background:rgba(200,134,10,.05);border-left:4px solid #C8860A;border-radius:0 12px 12px 0;
                        padding:1rem 1.25rem;margin-bottom:1.5rem;text-align:left;'>
                <p style='color:#A08060;font-size:.85rem;line-height:1.5;margin:0;'>
                    🔒 <strong style='color:#2C1A0E;'>Security tip:</strong> Never share this code with anyone.
                </p>
            </div>

            <!-- Footer -->
            <p style='color:#A08060;text-align:center;font-size:.8rem;line-height:1.6;
                      border-top:1px solid #E4D8C4;padding-top:1.25rem;margin:0;'>
                If you didn't create an account, you can safely ignore this email.
            </p>
        </div>

        <!-- Outer Footer -->
        <div style='max-width:480px;margin:1.25rem auto 0 auto;text-align:center;'>
            <p style='color:#A08060;font-size:.75rem;margin:0;'>
                &copy; 2026 ReadEase. All rights reserved.
            </p>
        </div>";

        await SendEmailAsync(toEmail, subject, body);
    }

    // ── Password Reset Email (unchanged) ──────────────────────────────────
    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
    {
        var subject = "Reset your ReadEase password";
        var body = $@"
            <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;'>
                <h2 style='color:#dc3545;'>Password Reset Request</h2>
                <p>Hi <strong>{userName}</strong>,</p>
                <p>We received a request to reset your password. Click the button below to proceed:</p>
                <div style='text-align:center;margin:30px 0;'>
                    <a href='{resetLink}'
                       style='background:#dc3545;color:white;padding:12px 30px;
                              text-decoration:none;border-radius:6px;font-size:16px;'>
                        Reset Password
                    </a>
                </div>
                <p style='color:#666;font-size:13px;'>
                    This link expires in <strong>1 hour</strong>.<br/>
                    If you did not request a password reset, please ignore this email.
                </p>
            </div>";

        await SendEmailAsync(toEmail, subject, body);
    }

    // ── Password Reset OTP Email ──────────────────────────────────────────
    public async Task SendPasswordResetOtpEmailAsync(string toEmail, string userName, string otp)
    {
        // Safety guard — OTP must be exactly 6 digits
        var safeOtp = otp?.Trim() ?? "";
        if (safeOtp.Length > 6) safeOtp = safeOtp.Substring(0, 6);

        var subject = "Your ReadEase password reset code";
        var body = $@"
        <div style='font-family:Georgia,serif;max-width:480px;margin:2rem auto;
                    background:#FFFCF7;border-radius:20px;padding:2.5rem 2rem;
                    border:2px solid #E4D8C4;box-shadow:0 8px 32px rgba(44,26,14,.1);'>

            <!-- Logo -->
            <div style='text-align:center;margin-bottom:1.5rem;'>
                <table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center'>
                    <tr>
                        <td style='width:75px;height:75px;border-radius:50%;border:3px dashed #C8860A;text-align:center;vertical-align:middle;background:#FFFCF7;'>
                            <div style='font-size:35px;line-height:75px;'>📖</div>
                        </td>
                    </tr>
                </table>
                <div style='font-size:2.1rem;font-weight:800;color:#C8860A;letter-spacing:-.02em;margin-top:1rem;'>
                    ReadEase
                </div>
                <div style='font-size:.8rem;color:#A08060;margin-top:.2rem;letter-spacing:.15em;text-transform:uppercase;'>
                    Your Reading Companion
                </div>
            </div>

            <!-- Gold Divider -->
            <div style='width:80px;height:4px;background:linear-gradient(90deg,#C8860A 0%,#E4D8C4 50%,#C8860A 100%);margin:1.5rem auto;border-radius:2px;'></div>

            <!-- Title -->
            <h2 style='color:#2C1A0E;text-align:center;font-size:1.5rem;font-weight:700;
                       margin:1.5rem 0 .75rem 0;font-family:Georgia,serif;letter-spacing:-.01em;'>
                Reset Your Password
            </h2>

            <!-- Message -->
            <p style='color:#A08060;text-align:center;font-size:1rem;line-height:1.7;
                      margin-bottom:2rem;padding:0 .5rem;'>
                Hi <strong style='color:#C8860A;'>{userName}</strong>, use this code to reset your password
            </p>

            <!-- OTP Container -->
            <div style='background:#F5EFE3;border:2px solid #C8860A;border-radius:18px;
                        padding:2rem 1.5rem;text-align:center;margin:0 auto 2rem auto;
                        max-width:350px;box-shadow:inset 0 2px 12px rgba(200,134,10,.06),0 6px 24px rgba(200,134,10,.12);'>
                <table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center'>
                    <tr>
                        <td style='text-align:center;'>
                            {string.Join("", safeOtp.Select(c => $@"
                            <span style='display:inline-block;width:52px;height:64px;
                                         background:#FFFCF7;border:2.5px solid #C8860A;border-radius:16px;
                                         font-size:2.2rem;font-weight:900;color:#C8860A;line-height:64px;
                                         text-align:center;font-family:''Courier New'',monospace;
                                         margin:0 3px;box-shadow:0 4px 12px rgba(200,134,10,.2);'>{c}</span>"))}
                        </td>
                    </tr>
                </table>
            </div>

            <!-- Timer Badge -->
            <div style='text-align:center;margin-bottom:1.5rem;'>
                <span style='display:inline-block;background:#F5EFE3;border:1px solid #E4D8C4;border-radius:25px;
                            padding:.5rem 1.25rem;font-size:.9rem;color:#A08060;'>
                    ⏱️ Expires in <strong style='color:#C8860A;font-weight:800;'>10 minutes</strong>
                </span>
            </div>

            <!-- Security Note -->
            <div style='background:rgba(200,134,10,.05);border-left:4px solid #C8860A;border-radius:0 12px 12px 0;
                        padding:1rem 1.25rem;margin-bottom:1.5rem;text-align:left;'>
                <p style='color:#A08060;font-size:.85rem;line-height:1.5;margin:0;'>
                    🔒 <strong style='color:#2C1A0E;'>Security tip:</strong> Never share this code with anyone.
                </p>
            </div>

            <!-- Footer -->
            <p style='color:#A08060;text-align:center;font-size:.8rem;line-height:1.6;
                      border-top:1px solid #E4D8C4;padding-top:1.25rem;margin:0;'>
                If you didn't request a password reset, you can safely ignore this email.
            </p>
        </div>
        
        <!-- Outer Footer -->
        <div style='max-width:480px;margin:1.25rem auto 0 auto;text-align:center;'>
            <p style='color:#A08060;font-size:.75rem;margin:0;'>
                &copy; 2026 ReadEase. All rights reserved.
            </p>
        </div>";

        await SendEmailAsync(toEmail, subject, body);
    }

    // ── Core SMTP sender ──────────────────────────────────────────────────
    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpHost = _config["Email:SmtpHost"];
        var smtpPort = _config["Email:SmtpPort"];
        var smtpUser = _config["Email:Username"];
        var smtpPass = _config["Email:Password"];
        var fromEmail = _config["Email:From"];
        var fromName = _config["Email:FromName"] ?? "ReadEase";

        _logger.LogInformation(
            "EMAIL CONFIG → Host={Host} Port={Port} User={User} From={From} HasPassword={HasPass}",
            smtpHost ?? "NULL", smtpPort ?? "NULL", smtpUser ?? "NULL",
            fromEmail ?? "NULL", !string.IsNullOrWhiteSpace(smtpPass));

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(smtpHost)) missing.Add("Email:SmtpHost");
        if (string.IsNullOrWhiteSpace(smtpPort)) missing.Add("Email:SmtpPort");
        if (string.IsNullOrWhiteSpace(smtpUser)) missing.Add("Email:Username");
        if (string.IsNullOrWhiteSpace(smtpPass)) missing.Add("Email:Password");
        if (string.IsNullOrWhiteSpace(fromEmail)) missing.Add("Email:From");

        if (missing.Count > 0)
        {
            var msg = $"Email config keys missing: {string.Join(", ", missing)}";
            _logger.LogError(msg);
            throw new InvalidOperationException(msg);
        }

        try
        {
            using var client = new SmtpClient(smtpHost, int.Parse(smtpPort!))
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 20_000
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail!, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            _logger.LogInformation("Sending email → To={To} Subject={Subject}", toEmail, subject);
            await client.SendMailAsync(message);
            _logger.LogInformation("✅ Email sent → {To}", toEmail);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx,
                "❌ SMTP error → To={To} Code={Code} Msg={Msg}",
                toEmail, smtpEx.StatusCode, smtpEx.Message);
            throw new InvalidOperationException(
                $"SMTP error ({smtpEx.StatusCode}): {smtpEx.Message}", smtpEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Email error → To={To} Type={Type} Msg={Msg}",
                toEmail, ex.GetType().Name, ex.Message);
            throw new InvalidOperationException(
                $"Email failed ({ex.GetType().Name}): {ex.Message}", ex);
        }
    }
}