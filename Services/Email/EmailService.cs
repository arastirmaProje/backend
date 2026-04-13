using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Personelim.Resources;

namespace Personelim.Services.Email;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public EmailService(
        IOptions<SmtpSettings> settings,
        ILogger<EmailService> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _settings = settings.Value;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<bool> SendBusinessVerificationCodeAsync(
        string email, string userName, string businessName, string code)
    {
        var body = GetBaseHtmlTemplate(
            _localizer["BusinessVerificationTitle"],
            $@"
            <p>{_localizer["EmailHello"]} <strong>{userName}</strong>,</p>
            <p><strong>{string.Format(_localizer["VerificationCodeFor"], businessName)}</strong></p>
            <h2 style='letter-spacing:4px'>{code}</h2>"
        );
        return await SendAsync(email, $"{businessName} - {_localizer["BusinessVerificationTitle"]}", body);
    }

    public async Task<bool> SendPasswordResetCodeAsync(
        string email, string code, string userName)
    {
        var body = GetBaseHtmlTemplate(
            _localizer["PasswordResetTitle"],
            $"<p>{_localizer["EmailHello"]} {userName},</p><h2>{code}</h2>"
        );
        return await SendAsync(email, _localizer["PasswordResetSubject"], body);
    }

    public async Task<bool> SendAccountCreatedEmailAsync(
        string email, string firstName, string plainPassword, string businessName = null)
    {
        var body = GetBaseHtmlTemplate(
            _localizer["AccountCreatedTitle"],
            $"<p>{_localizer["EmailHello"]} {firstName},</p><p>{_localizer["PasswordField"]} <b>{plainPassword}</b></p>"
        );
        return await SendAsync(email, _localizer["AccountCreatedSubject"], body);
    }

    public async Task<bool> SendAddedToBusinessEmailAsync(
        string email, string firstName, string businessName)
    {
        var body = GetBaseHtmlTemplate(
            _localizer["NewBusinessTitle"],
            $"<p>{string.Format(_localizer["BusinessAddedYou"], businessName)}</p>"
        );
        return await SendAsync(email, string.Format(_localizer["BusinessAddedSubject"], businessName), body);
    }

    public async Task<bool> SendInvitationEmailAsync(
        string email, string invitationCode, string businessName, string inviterName, string message)
    {
        var content = $@"
            <p>{string.Format(_localizer["BusinessInvitingYou"], businessName)}</p>
            {(string.IsNullOrWhiteSpace(message) ? "" : $"<p><em>{message}</em></p>")}
            <p>{_localizer["EmailHello"]}, <strong>{inviterName}</strong> sizi davet etti.</p>
            <p>Davet kodunuz:</p>
            <h2 style='letter-spacing:4px'>{invitationCode}</h2>";

        var body = GetBaseHtmlTemplate(_localizer["BusinessInvitationTitle"], content);
        return await SendAsync(email, _localizer["BusinessInvitationTitle"], body);
    }

    private async Task<bool> SendAsync(string to, string subject, string html)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = html };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP email error. To: {To}, Subject: {Subject}", to, subject);
            return false;
        }
    }

    private string GetBaseHtmlTemplate(string title, string content)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee;'>
                <h2 style='color: #2c3e50;'>{title}</h2>
                <hr style='border: 0; border-top: 1px solid #eee;' />
                <div style='padding: 20px 0;'>
                    {content}
                </div>
            </div>
        </body>
        </html>";
    }
}
