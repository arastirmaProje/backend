using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Personelim.Services.Email
{
    // IEmailService implementasyonunuz burada varsayılmıştır.
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // =======================================================
        // GÖNDERİM METODLARI (Bu kısım aynı kalabilir)
        // =======================================================

        public async Task<bool> SendBusinessVerificationCodeAsync(string email, string userName, string businessName, string code)
        {
            string title = "İşletme Doğrulama Kodu";
            string content = $@"
                <p>Merhaba sn. <strong>{userName}</strong>,</p>
                <p><strong>{businessName}</strong> adlı işletme kaydınız alınmıştır. İşlemi tamamlamak için aşağıdaki doğrulama kodunu kullanınız.</p>
                
                <div style='background-color: #e3f2fd; border-left: 4px solid #2196f3; padding: 20px; text-align: center; margin: 30px 0;'>
                    <span style='display: block; font-size: 14px; color: #546e7a; margin-bottom: 5px;'>DOĞRULAMA KODUNUZ</span>
                    <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #0d47a1; font-family: monospace;'>{code}</span>
                </div>
                
                <p style='font-size: 13px; color: #6c757d;'>Bu kod güvenliğiniz içindir, lütfen kimseyle paylaşmayınız.</p>";
            
            var body = GetBaseHtmlTemplate(title, content);
            return await SendEmailBaseAsync(email, $"{businessName} - Doğrulama Kodu", body);
        }

        public async Task<bool> SendPasswordResetCodeAsync(string email, string code, string userName)
        {
            string content = $@"
                <p>Merhaba sn. <strong>{userName}</strong>,</p>
                <p>Hesabınızın şifresini sıfırlamak için aşağıdaki doğrulama kodunu kullanabilirsiniz.</p>
                <div style='background-color: #f8f9fa; border-left: 4px solid #4a90e2; padding: 20px; text-align: center; margin: 30px 0;'>
                    <span style='display: block; font-size: 14px; color: #6c757d; margin-bottom: 5px;'>DOĞRULAMA KODUNUZ</span>
                    <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #2c3e50; font-family: monospace;'>{code}</span>
                </div>
                <p style='font-size: 13px; color: #6c757d;'>Bu kod 15 dakika geçerlidir.</p>";
            
            var body = GetBaseHtmlTemplate("Şifre Sıfırlama", content);
            return await SendEmailBaseAsync(email, "Şifre Sıfırlama Kodu", body);
        }

        public async Task<bool> SendAccountCreatedEmailAsync(string email, string firstName, string plainPassword, string businessName = null)
        {
            string title = "Personelim Hesabınız Oluşturuldu";
            string intro = !string.IsNullOrEmpty(businessName)
                ? $"<p><strong>{businessName}</strong> işletmesi sizi ekibine dahil etti ve sizin için bir hesap oluşturuldu.</p>"
                : "<p>Sistemde kaydınız oluşturuldu.</p>";
            
            string content = $@"
                <p>Merhaba <strong>{firstName}</strong>,</p>
                {intro}
                <p>Giriş yapabilmeniz için geçici şifreniz aşağıdadır. Lütfen giriş yaptıktan sonra şifrenizi değiştiriniz.</p>
                
                <div style='background-color: #fff3cd; border: 1px solid #ffeeba; border-radius: 6px; padding: 20px; margin: 25px 0; text-align: left;'>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                            <td style='padding: 5px 0; color: #856404; font-weight: bold; width: 80px;'>E-posta:</td>
                            <td style='padding: 5px 0; color: #333;'>{email}</td>
                        </tr>
                        <tr>
                            <td style='padding: 5px 0; color: #856404; font-weight: bold;'>Şifre:</td>
                            <td style='padding: 5px 0; font-family: monospace; font-size: 20px; color: #d63031; font-weight: bold; letter-spacing: 1px;'>{plainPassword}</td>
                        </tr>
                    </table>
                </div>
                
                <div style='text-align: center; margin-top: 30px;'>
                    <p style='font-size: 14px; color: #7f8c8d;'>Bu şifre sisteme tarafından otomatik oluşturulmuştur.</p>
                </div>";
            
            var body = GetBaseHtmlTemplate(title, content);
            string subject = !string.IsNullOrEmpty(businessName) ? $"{businessName} Sizi Ekibine Ekledi - Giriş Bilgileri" : "Personelim Giriş Bilgileri";

            return await SendEmailBaseAsync(email, subject, body);
        }

        public async Task<bool> SendAddedToBusinessEmailAsync(string email, string firstName, string businessName)
        {
            string content = $@"
                <p>Merhaba <strong>{firstName}</strong>,</p>
                <p>Hayırlı olsun! <strong>{businessName}</strong> işletmesi sizi personel listesine ekledi.</p>
                <p>Mevcut e-posta adresiniz ve şifrenizle uygulamaya giriş yaparak yeni işletmenizi görüntüleyebilirsiniz.</p>
                
                <div style='text-align: center; margin: 30px 0;'>
                     <p style='color:#27ae60; font-weight:bold;'>Hesabınız zaten aktif, ekstra işlem yapmanıza gerek yoktur.</p>
                </div>";
            
            var body = GetBaseHtmlTemplate("Yeni Bir Ekibe Katıldınız", content);
            return await SendEmailBaseAsync(email, $"{businessName} Sizi Ekledi", body);
        }

        public async Task<bool> SendInvitationEmailAsync(string email, string invitationCode, string businessName, string inviterName, string message)
        {
            // Davet e-postası implementasyonu buraya eklenecektir.
            // Örnek:
            // string content = $@"<p>Merhaba, {businessName} işletmesi sizi davet ediyor.</p>";
            // var body = GetBaseHtmlTemplate("İşletme Daveti", content);
            // return await SendEmailBaseAsync(email, "İşletme Daveti", body);

            // Geçici olarak true döndürülmüş.
            return await System.Threading.Tasks.Task.FromResult(true); 
        }


        // =======================================================
        // KRİTİK KISIM: SendEmailBaseAsync (MailKit ile)
        // =======================================================
        private async Task<bool> SendEmailBaseAsync(string toEmail, string subject, string htmlBody)
        {
            // Appsettings.json dosyasından okuma
            var host = _configuration["Email:SmtpHost"];        
            var portString = _configuration["Email:SmtpPort"];  
            var fromEmail = _configuration["Email:SmtpUser"];   
            var password = _configuration["Email:SmtpPass"];    
            var displayName = _configuration["Email:FromName"] ?? "Personelim App";

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password))
            {
                _logger.LogError("Email ayarları eksik: Host, Kullanıcı veya Şifre bulunamadı.");
                return false;
            }

            if (!int.TryParse(portString, out int port)) port = 587; // Varsayılan: 587

            // MimeMessage oluşturma (Gönderi içeriğini MailKit formatında hazırlar)
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(displayName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            
            message.Body = new TextPart("html") { Text = htmlBody };

            try
            {
                // SmtpClient oluşturma (MailKit)
                using (var client = new SmtpClient())
                {
                    // SecureSocketOptions.Auto: Port 587 ise STARTTLS'i doğru şekilde dener. 
                    // Port 465 ise Implicit SSL/TLS kullanır. Bu sayede ayarlarınız esnek olur.
                    await client.ConnectAsync(host, port, SecureSocketOptions.Auto); 

                    // Sunucuya kimlik doğrulama
                    await client.AuthenticateAsync(fromEmail, password);

                    // E-posta gönderme
                    await client.SendAsync(message);
                    
                    // Bağlantıyı kes
                    await client.DisconnectAsync(true);
                    
                    _logger.LogInformation($"✅ Mail başarıyla gönderildi: {toEmail}");
                    return true;
                }
            }
            catch (AuthenticationException authEx)
            {
                _logger.LogError(authEx, $"❌ SMTP Kimlik Doğrulama Hatası. Kullanıcı: {fromEmail}");
                // Kimlik doğrulama hatası genellikle yanlış şifre (Uygulama Şifresi sorunu) veya kullanıcı adından kaynaklanır.
                throw new Exception($"SMTP Kimlik Doğrulama Hatası: Şifre veya Kullanıcı Adı yanlış. | DETAY: {authEx.Message}", authEx);
            }
            catch (SmtpCommandException cmdEx)
            {
                _logger.LogError(cmdEx, $"❌ SMTP Komut Hatası. Hata Kodu: {cmdEx.ErrorCode}");
                throw new Exception($"SMTP Komut Hatası: Sunucu bağlantıyı reddetti. | DETAY: {cmdEx.Message}", cmdEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ GENEL SMTP Hatası. Alıcı: {toEmail}");
                // Diğer genel hatalar (Ağ sorunu, DNS, zaman aşımı vb.)
                throw new Exception($"SMTP Hatası: Failure sending mail. | DETAY: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
        }

        // =======================================================
        // HTML TEMPLATE (Aynı kalabilir)
        // =======================================================
        private string GetBaseHtmlTemplate(string title, string content)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: 'Segoe UI', sans-serif; background-color: #f4f6f8; margin: 0; padding: 0; }}
                    .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05); }}
                    .header {{ background: #2c3e50; padding: 20px; text-align: center; color: white; }}
                    .content {{ padding: 30px; color: #34495e; line-height: 1.6; }}
                    .footer {{ background: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #95a5a6; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Personelim</h1>
                    </div>
                    <div class='content'>
                        <h2 style='color:#2c3e50; border-bottom:1px solid #eee; padding-bottom:10px;'>{title}</h2>
                        {content}
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Personelim Uygulaması</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}