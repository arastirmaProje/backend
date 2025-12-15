using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Personelim.Services.Email;

public class EmailService : IEmailService
{
    private readonly SendGridSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly SendGridClient _client;

    public EmailService(
        IOptions<SendGridSettings> settings,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _client = new SendGridClient(_settings.ApiKey);
    }

    // =======================================================
    // PUBLIC METHODS (AYNI – BUSINESS BOZULMAZ)
    // =======================================================

    public async Task<bool> SendBusinessVerificationCodeAsync(
        string email, string userName, string businessName, string code)
    {
        var body = GetBaseHtmlTemplate(
            "İşletme Doğrulama Kodu",
            $@"
            <p>Merhaba <strong>{userName}</strong>,</p>
            <p><strong>{businessName}</strong> için doğrulama kodunuz:</p>
            <h2 style='letter-spacing:4px'>{code}</h2>"
        );

        return await SendAsync(email, $"{businessName} - Doğrulama Kodu", body);
    }

    public async Task<bool> SendPasswordResetCodeAsync(
        string email, string code, string userName)
    {
        var body = GetBaseHtmlTemplate(
            "Şifre Sıfırlama",
            $"<p>Merhaba {userName},</p><h2>{code}</h2>"
        );

        return await SendAsync(email, "Şifre Sıfırlama Kodu", body);
    }

    public async Task<bool> SendAccountCreatedEmailAsync(
        string email, string firstName, string plainPassword, string businessName = null)
    {
        var body = GetBaseHtmlTemplate(
            "Hesabınız Oluşturuldu",
            $"<p>Merhaba {firstName},</p><p>Şifre: <b>{plainPassword}</b></p>"
        );

        return await SendAsync(email, "Personelim Hesabınız Oluşturuldu", body);
    }

    public async Task<bool> SendAddedToBusinessEmailAsync(
        string email, string firstName, string businessName)
    {
        var body = GetBaseHtmlTemplate(
            "Yeni İşletme",
            $"<p>{businessName} sizi ekibine ekledi.</p>"
        );

        return await SendAsync(email, $"{businessName} Sizi Ekledi", body);
    }

    public async Task<bool> SendInvitationEmailAsync(
        string email, string invitationCode, string businessName, string inviterName, string message)
    {
        var body = GetBaseHtmlTemplate(
            "İşletme Daveti",
            $"<p>{businessName} sizi davet ediyor.</p>"
        );

        return await SendAsync(email, "İşletme Daveti", body);
    }

    // =======================================================
    // CORE SEND
    // =======================================================
    private async Task<bool> SendAsync(string to, string subject, string html)
    {
        try
        {
            var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
            var toEmail = new EmailAddress(to);

            var msg = MailHelper.CreateSingleEmail(
                from,
                toEmail,
                subject,
                plainTextContent: null,
                htmlContent: html
            );

            var response = await _client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
            {
                _logger.LogError(
                    "SendGrid error {Status}",
                    response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid email error");
            throw;
        }
    }

    // =======================================================
    // HTML TEMPLATE (AYNI)
    // =======================================================
    private string GetBaseHtmlTemplate(string title, string content)
    {
        return $@"
        <html>
        <body>
            <h2>{title}</h2>
            {content}
        </body>
        </html>";
    }
}