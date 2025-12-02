using MailKit.Net.Smtp; // MailKit kütüphanesi
using MailKit.Security; 
using MimeKit;          
using MimeKit.Text;     
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

        // --- 1. ŞİFRE SIFIRLAMA ---
        public async Task<bool> SendPasswordResetCodeAsync(string email, string code, string userName)
        {
            string content = $@"
                <p>Merhaba sn. <strong>{userName}</strong>,</p>
                <p>Hesabınızın şifresini sıfırlamak için aşağıdaki doğrulama kodunu kullanabilirsiniz.</p>
                
                <div style='background-color: #f8f9fa; border-left: 4px solid #4a90e2; padding: 20px; text-align: center; margin: 30px 0;'>
                    <span style='display: block; font-size: 14px; color: #6c757d; margin-bottom: 5px;'>DOĞRULAMA KODUNUZ</span>
                    <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #2c3e50; font-family: monospace;'>{code}</span>
                </div>
                
                <p style='font-size: 13px; color: #6c757d;'>Bu kod 15 dakika boyunca geçerlidir. Talebi siz yapmadıysanız lütfen dikkate almayınız.</p>";

            var body = GetBaseHtmlTemplate("Şifre Sıfırlama", content);
            return await SendEmailBaseAsync(email, "Şifre Sıfırlama Kodu - Personelim", body);
        }

        // --- 2. DAVETİYE ---
        public async Task<bool> SendInvitationEmailAsync(string email, string invitationCode, string businessName, string inviterName, string message)
        {
            string messageHtml = string.IsNullOrEmpty(message) ? "" : $"<p style='font-style: italic; color: #555;'>\"{message}\"</p>";
            
            string content = $@"
                <p>Merhaba,</p>
                <p><strong>{inviterName}</strong>, sizi <strong>{businessName}</strong> ekibine katılmaya davet etti.</p>
                {messageHtml}
                
                <div style='text-align: center; margin: 30px 0;'>
                   <div style='background-color: #ecf0f1; padding: 15px; border-radius: 5px; display: inline-block;'>
                        <span style='font-size: 14px; color: #7f8c8d; display: block;'>Davet Kodu</span>
                        <span style='font-size: 24px; font-weight: bold; color: #2980b9;'>{invitationCode}</span>
                   </div>
                </div>";

            var body = GetBaseHtmlTemplate("İşletme Daveti", content);
            return await SendEmailBaseAsync(email, $"{businessName} İşletmesi İçin Davet", body);
        }

        // --- 3. YENİ HESAP OLUŞTURMA ---
        public async Task<bool> SendAccountCreatedEmailAsync(string email, string firstName, string plainPassword, string businessName = null)
        {
            string welcomeText = !string.IsNullOrEmpty(businessName)
                ? $"<strong>{businessName}</strong> işletmesi sizi ekibine ekledi."
                : "Personelim uygulamasında hesabınız oluşturuldu.";

            string content = $@"
                <p>Merhaba <strong>{firstName}</strong>,</p>
                <p>{welcomeText}</p>
                <p>Sizin için oluşturulan geçici giriş bilgileri aşağıdadır:</p>

                <div style='background-color: #fff3cd; border: 1px solid #ffeeba; border-radius: 6px; padding: 20px; margin: 25px 0;'>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                            <td style='padding: 5px 0; color: #856404; font-weight: bold; width: 80px;'>E-posta:</td>
                            <td style='padding: 5px 0; color: #333;'>{email}</td>
                        </tr>
                        <tr>
                            <td style='padding: 5px 0; color: #856404; font-weight: bold;'>Şifre:</td>
                            <td style='padding: 5px 0; font-family: monospace; font-size: 18px; color: #d63031; font-weight: bold;'>{plainPassword}</td>
                        </tr>
                    </table>
                </div>

                <p style='color: #e74c3c; font-size: 14px;'><strong>Önemli:</strong> Güvenliğiniz için lütfen ilk girişinizde şifrenizi değiştiriniz.</p>";

            var body = GetBaseHtmlTemplate("Aramıza Hoşgeldiniz!", content);
            string subject = !string.IsNullOrEmpty(businessName) ? $"{businessName} Ekibine Hoşgeldiniz" : "Personelim Hesabınız";
            
            return await SendEmailBaseAsync(email, subject, body);
        }

        // --- 4. MEVCUT KULLANICI EKLEME ---
        public async Task<bool> SendAddedToBusinessEmailAsync(string email, string firstName, string businessName)
        {
            string content = $@"
                <p>Merhaba <strong>{firstName}</strong>,</p>
                <p>Hayırlı olsun! <strong>{businessName}</strong> işletmesi sizi personel listesine ekledi.</p>
                <p>Mevcut e-posta adresiniz ve şifrenizle sisteme giriş yaparak işletme paneline erişebilirsiniz.</p>
                
                <div style='text-align: center; margin: 30px 0;'>
                     <a href='#' style='background-color: #27ae60; color: white; padding: 12px 25px; text-decoration: none; border-radius: 4px; font-weight: bold; font-size: 16px;'>Uygulamaya Git</a>
                </div>";

            var body = GetBaseHtmlTemplate("Yeni Bir Ekibe Katıldınız", content);
            return await SendEmailBaseAsync(email, $"{businessName} İşletmesine Eklendiniz", body);
        }

        // --- GÜNCELLENMİŞ MAILKIT GÖNDERİM METODU ---
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

                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(fromName, fromEmail));
                emailMessage.To.Add(new MailboxAddress(toEmail, toEmail));
                emailMessage.Subject = subject;
                emailMessage.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mail gönderilemedi: {Email}. Hata: {Message}", toEmail, ex.Message);
                return false;
            }
        }

        // --- PROFESYONEL HTML ŞABLONU (HELPER) ---
        // Bu metot, gönderilecek içeriği standart bir çerçeve (header, footer, container) içine alır.
        private string GetBaseHtmlTemplate(string title, string content)
        {
            var year = DateTime.Now.Year;
            
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f4f6f8; }}
                    .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.05); overflow: hidden; }}
                    .header {{ background-color: #2c3e50; padding: 25px; text-align: center; }}
                    .header h1 {{ color: #ffffff; margin: 0; font-size: 24px; font-weight: 600; letter-spacing: 1px; }}
                    .content {{ padding: 40px 30px; color: #34495e; line-height: 1.6; font-size: 16px; }}
                    .content h2 {{ color: #2c3e50; margin-top: 0; font-size: 22px; border-bottom: 2px solid #ecf0f1; padding-bottom: 15px; margin-bottom: 25px; }}
                    .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #95a5a6; border-top: 1px solid #ecf0f1; }}
                </style>
            </head>
            <body style='margin: 0; padding: 0; background-color: #f4f6f8; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
                
                <table role='presentation' border='0' cellspacing='0' width='100%'>
                    <tr>
                        <td style='padding: 20px 0; text-align: center;'>
                            <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); overflow: hidden; text-align: left;'>
                                
                                <!-- HEADER -->
                                <div style='background-color: #2c3e50; padding: 25px; text-align: center;'>
                                    <h1 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 600;'>Personelim</h1>
                                </div>

                                <!-- BODY -->
                                <div style='padding: 40px 30px; color: #34495e; line-height: 1.6;'>
                                    <h2 style='color: #2c3e50; margin-top: 0; font-size: 20px; border-bottom: 1px solid #eee; padding-bottom: 10px; margin-bottom: 20px;'>{title}</h2>
                                    {content}
                                </div>

                                <!-- FOOTER -->
                                <div style='background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #95a5a6; border-top: 1px solid #eee;'>
                                    <p style='margin: 5px 0;'>&copy; {year} Personelim Uygulaması</p>
                                    <p style='margin: 0;'>Bu e-posta otomatik olarak gönderilmiştir, lütfen yanıtlamayınız.</p>
                                </div>
                            </div>
                        </td>
                    </tr>
                </table>

            </body>
            </html>";
        }
    }
}