using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Personelim.Resources;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Personelim.Services.Email;

public class EmailService : IEmailService
{
    private readonly SendGridSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly SendGridClient _client;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public EmailService(
        IOptions<SendGridSettings> settings,
        ILogger<EmailService> logger,
        IStringLocalizer<SharedResource> localizer)
    {
        _settings = settings.Value;
        _logger = logger;
        _localizer = localizer;
        _client = new SendGridClient(_settings.ApiKey);
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
        var body = GetBaseHtmlTemplate(
            _localizer["BusinessInvitationTitle"],
            $"<p>{string.Format(_localizer["BusinessInvitingYou"], businessName)}</p>"
        );
        return await SendAsync(email, _localizer["BusinessInvitationTitle"], body);
    }
    
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
                var responseBody = await response.Body.ReadAsStringAsync(); 
                _logger.LogError(
                    "SendGrid Error. Status: {Status}. Body: {Body}",
                    response.StatusCode,
                    responseBody); 
                
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid email error occurred in SendAsync.");
            throw; 
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