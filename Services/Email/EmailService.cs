using MailKit.Net.Smtp; 
using MailKit.Security; 
using MimeKit;          
using MimeKit.Text;     
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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
        
        // --- HTTP API GÖNDERİM METODU (%100 GARANTİLİ) ---
private async Task<bool> SendEmailBaseAsync(string toEmail, string subject, string htmlBody)
{
    try
    {
        // 1. API KEY'İ ÇEK (SMTP Şifresi olarak kaydettiğin xsmtpsib... anahtarı)
        // Önce Render Environment'a bak, yoksa yerel config'e bak.
        var apiKey = Environment.GetEnvironmentVariable("Email__SmtpPass") ?? _configuration["Email:SmtpPass"];
        
        var senderName = "Personelim";
        var senderEmail = "furkanozkan20001@gmail.com"; // Brevo'da onaylı mailin

        // 2. GÖNDERİLECEK VERİ MODELİ
        var payload = new
        {
            sender = new { name = senderName, email = senderEmail },
            to = new[] { new { email = toEmail } },
            subject = subject,
            htmlContent = htmlBody
        };

        // 3. HTTP İSTEĞİ HAZIRLA
        var jsonContent = JsonSerializer.Serialize(payload);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var httpClient = new HttpClient();
        
        // Brevo'ya Şifreyi Göster
        httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // 4. İSTEĞİ POST ET (URL: SMTP değil, API)
        var url = "https://api.brevo.com/v3/smtp/email";
        var response = await httpClient.PostAsync(url, httpContent);

        // 5. SONUCU KONTROL ET
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation($"✅ Mail API ile başarıyla gönderildi: {toEmail}");
            return true;
        }
        else
        {
            // Hata varsa ne olduğunu okuyalım
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError($"❌ API Hatası: {response.StatusCode} - Detay: {errorBody}");
            return false;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "HTTP İsteği Başarısız: {Message}", ex.Message);
        return false;
    }
}
    
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