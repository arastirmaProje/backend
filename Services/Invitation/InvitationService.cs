using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Invitation;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using Personelim.Services.Email;
using BCrypt.Net; // Hashleme için gerekli

namespace Personelim.Services.Invitation
{
    public class InvitationService : IInvitationService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public InvitationService(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<ServiceResponse<InvitationResponse>> SendInvitationAsync(Guid userId, SendInvitationRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Yetki Kontrolü
                var isOwner = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == userId &&
                    bm.BusinessId == request.BusinessId &&
                    bm.Role == UserRole.Owner &&
                    bm.IsActive);

                if (!isOwner)
                {
                    return ServiceResponse<InvitationResponse>.ErrorResult("Personel ekleme yetkiniz yok.");
                }

                var business = await _context.Businesses.FindAsync(request.BusinessId);
                var inviter = await _context.Users.FindAsync(userId);
                var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

                bool isNewUser = false;
                string generatedPassword = null; // Ham şifreyi burada tutacağız

                // 2. Kullanıcı Yoksa Oluştur (Şifre Üret)
                if (targetUser == null)
                {
                    isNewUser = true;
                    // Rastgele şifre üret (EmailService'e bu gidecek)
                    generatedPassword = GenerateRandomPassword(); 
                    
                    // DB'ye kaydetmek için hashle
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(generatedPassword);

                    // İsim Soyisim mailden türetme (ali.veli@gmail -> Ali Veli)
                    string nameFromEmail = request.Email.Split('@')[0];
                    
                    targetUser = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = request.Email.ToLower(),
                        FirstName = nameFromEmail, 
                        LastName = "",
                        PasswordHash = passwordHash,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _context.Users.AddAsync(targetUser);
                }
                else
                {
                    // Kullanıcı zaten varsa, bu işletmede mi diye bak
                    var isMember = await _context.BusinessMembers.AnyAsync(bm =>
                        bm.UserId == targetUser.Id &&
                        bm.BusinessId == request.BusinessId &&
                        bm.IsActive);

                    if (isMember)
                    {
                        return ServiceResponse<InvitationResponse>.ErrorResult("Bu kullanıcı zaten personeliniz.");
                    }
                }

                // 3. Personel Olarak Ekle
                var member = new Models.BusinessMember
                {
                    BusinessId = request.BusinessId,
                    UserId = targetUser.Id,
                    Role = UserRole.Employee,
                    Position = "Personel",
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _context.BusinessMembers.AddAsync(member);

                // 4. Log Amaçlı Invitation Tablosuna Yaz (Opsiyonel ama iyi olur)
                var logEntry = new Models.Invitation
                {
                    BusinessId = business.Id,
                    Email = request.Email,
                    InvitedByUserId = userId,
                    Status = InvitationStatus.Accepted, // Direkt kabul edilmiş sayıyoruz
                    Message = request.Message ?? "Doğrudan eklendi",
                    InvitationCode = "DIRECT-" + Guid.NewGuid().ToString().Substring(0,6),
                    CreatedAt = DateTime.UtcNow,
                    AcceptedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
                _context.Invitations.Add(logEntry);

                await _context.SaveChangesAsync();

                // 5. MAİL GÖNDERİMİ
                // Transaction commit etmeden maili gönderelim (veya sonra)
                bool mailSent = false;
                string inviterName = $"{inviter.FirstName} {inviter.LastName}";

                if (isNewUser)
                {
                    // Şifre içeren maili atıyoruz
                    mailSent = await _emailService.SendAccountCreatedEmailAsync(
                        targetUser.Email, 
                        targetUser.FirstName, 
                        generatedPassword, // Ham şifreyi gönderiyoruz
                        business.Name
                    );
                }
                else
                {
                    // Bilgilendirme maili atıyoruz
                    mailSent = await _emailService.SendAddedToBusinessEmailAsync(
                        targetUser.Email, 
                        targetUser.FirstName, 
                        business.Name
                    );
                }

                await transaction.CommitAsync();

                string msg = isNewUser 
                    ? "Yeni kullanıcı oluşturuldu, personellere eklendi ve şifresi mail atıldı." 
                    : "Mevcut kullanıcı personellere eklendi ve bilgilendirildi.";

                if (!mailSent) msg += " (Ancak mail gönderilirken hata oluştu)";

                // Response dönüyoruz (Frontend bu veriyi kullanabilir)
                return ServiceResponse<InvitationResponse>.SuccessResult(new InvitationResponse 
                {
                    Id = logEntry.Id,
                    Email = targetUser.Email,
                    Message = "İşlem Başarılı"
                }, msg);

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResponse<InvitationResponse>.ErrorResult("Hata oluştu: " + ex.Message);
            }
        }

        // 8 Haneli Rastgele Şifre Üretici
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
        }

       
        public Task<ServiceResponse<string>> AcceptInvitationAsync(Guid userId, string code) => throw new NotImplementedException();
        public Task<ServiceResponse<string>> CancelInvitationAsync(Guid userId, Guid id) => throw new NotImplementedException();
        public async Task<ServiceResponse<List<InvitationResponse>>> GetUserInvitationsAsync(string email) 
        {
            return ServiceResponse<List<InvitationResponse>>.SuccessResult(new List<InvitationResponse>());
        }
    }
}