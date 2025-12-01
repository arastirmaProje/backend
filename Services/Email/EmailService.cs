using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Personelim.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // --- 1. ŞİFRE SIFIRLAMA KODU ---
        public async Task<bool> SendPasswordResetCodeAsync(string email, string code, string userName)
        {
            string subject = "Şifre Sıfırlama Kodu - Personelim";
            string body = GetPasswordResetBody(userName, code);
            return await SendEmailBaseAsync(email, subject, body);
        }

        // --- 2. DAVETİYE MAİLİ (InvitationService İÇİN GERİ GELDİ) ---
        public async Task<bool> SendInvitationEmailAsync(string email, string invitationCode, string businessName, string inviterName, string message)
        {
            string subject = $"{businessName} İşletmesi İçin Davetiyeniz Var - Personelim";
            string body = GetInvitationBody(businessName, inviterName, invitationCode, message);
            return await SendEmailBaseAsync(email, subject, body);
        }

        // --- 3. YENİ HESAP OLUŞTURMA (ŞİFRE İÇERİR - BusinessMemberService İÇİN) ---
        public async Task<bool> SendAccountCreatedEmailAsync(string email, string firstName, string plainPassword, string businessName = null)
        {
            string subject = !string.IsNullOrEmpty(businessName) 
                ? $"{businessName} Ekibine Hoşgeldiniz - Personelim" 
                : "Personelim Hesabınız Oluşturuldu";

            string body = GetAccountCreatedBody(firstName, email, plainPassword, businessName);
            return await SendEmailBaseAsync(email, subject, body);
        }

        // --- 4. MEVCUT KULLANICIYI EKLEME (BusinessMemberService İÇİN) ---
        public async Task<bool> SendAddedToBusinessEmailAsync(string email, string firstName, string businessName)
        {
            string subject = $"{businessName} İşletmesine Eklendiniz - Personelim";
            string body = GetAddedToBusinessBody(firstName, businessName);
            return await SendEmailBaseAsync(email, subject, body);
        }

        // --- ANA GÖNDERİM METODU (HEPSİ BUNU KULLANIR) ---
        private async Task<bool> SendEmailBaseAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
                var smtpUser = _configuration["Email:SmtpUser"];
                var smtpPass = _configuration["Email:SmtpPass"];
                var fromEmail = _configuration["Email:FromEmail"];
                var fromName = _configuration["Email:FromName"];

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email gönderilemedi: {Email}", toEmail);
                return false;
            }
        }

        // --- HTML TASARIMLARI ---

        private string GetPasswordResetBody(string userName, string code)
        {
            return $@"
                <div style='font-family: Arial;'>
                    <h3>Şifre Sıfırlama</h3>
                    <p>Merhaba {userName}, kodunuz:</p>
                    <h2 style='color:#007bff; letter-spacing:5px;'>{code}</h2>
                </div>";
        }

        private string GetInvitationBody(string businessName, string inviterName, string code, string message)
        {
             string msgHtml = string.IsNullOrEmpty(message) ? "" : $"<p><em>\"{message}\"</em></p>";
             return $@"
                <div style='font-family: Arial;'>
                    <h3>İşletme Daveti</h3>
                    <p><strong>{inviterName}</strong> sizi <strong>{businessName}</strong> ekibine davet etti.</p>
                    {msgHtml}
                    <p>Davet Kodu:</p>
                    <h2 style='color:#28a745; letter-spacing:2px;'>{code}</h2>
                </div>";
        }

        private string GetAccountCreatedBody(string firstName, string email, string password, string businessName)
        {
            string welcome = !string.IsNullOrEmpty(businessName) ? $"{businessName} sizi ekledi." : "Hesabınız açıldı.";
            return $@"
                <div style='font-family: Arial;'>
                    <h3>Hoşgeldiniz {firstName}</h3>
                    <p>{welcome}</p>
                    <div style='background:#eee; padding:15px;'>
                        <p>Email: {email}</p>
                        <p>Şifre: <b style='color:red'>{password}</b></p>
                    </div>
                    <p>Lütfen şifrenizi değiştirin.</p>
                </div>";
        }

        private string GetAddedToBusinessBody(string firstName, string businessName)
        {
            return $@"
                <div style='font-family: Arial;'>
                    <h3>Yeni Ekip Bildirimi</h3>
                    <p>Merhaba {firstName},</p>
                    <p><strong>{businessName}</strong> işletmesi sizi ekibe dahil etti.</p>
                    <p>Mevcut şifrenizle giriş yapabilirsiniz.</p>
                </div>";
        }
    }
}