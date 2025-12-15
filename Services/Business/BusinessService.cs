using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Personelim.Data;
using Personelim.DTOs.Business;
using Personelim.Helpers;
using Personelim.Services.Email;

namespace Personelim.Services.Business
{
    public class BusinessService : IBusinessService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<BusinessService> _logger; // Loglama için eklendi

        public BusinessService(AppDbContext context, IEmailService emailService, ILogger<BusinessService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ServiceResponse<List<BusinessResponse>>> GetUserBusinessesAsync(Guid? userId)
        {
            if (!userId.HasValue)
                return ServiceResponse<List<BusinessResponse>>.SuccessResult(new List<BusinessResponse>());
            
            // İsterseniz burada doğrulanmamış işletmeleri de göstermek için IsActive filtresini kaldırabilirsiniz.
            // Şimdilik sadece Aktif olanları getiriyor.
            var businesses = await _context.Businesses
                .Where(b => b.OwnerId == userId && b.IsActive && b.ParentBusinessId == null)
                .Select(b => new BusinessResponse { Id = b.Id, Name = b.Name })
                .ToListAsync();
            return ServiceResponse<List<BusinessResponse>>.SuccessResult(businesses);
        }

        public async Task<ServiceResponse<BusinessResponse>> GetBusinessByIdAsync(Guid? userId, Guid businessId)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);
            if (business == null) return ServiceResponse<BusinessResponse>.ErrorResult("İşletme bulunamadı");
            return ServiceResponse<BusinessResponse>.SuccessResult(new BusinessResponse
            {
                Id = business.Id,
                Name = business.Name
            });
        }

        public async Task<ServiceResponse<BusinessResponse>> UpdateBusinessAsync(Guid? userId, Guid businessId, UpdateBusinessRequest request)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);
            if (business == null) return ServiceResponse<BusinessResponse>.ErrorResult("İşletme bulunamadı");
            if (!string.IsNullOrWhiteSpace(request.Name)) business.Name = request.Name.Trim();
            if (!string.IsNullOrWhiteSpace(request.Description)) business.Description = request.Description.Trim();
            if (!string.IsNullOrWhiteSpace(request.Address)) business.Address = request.Address.Trim();
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber)) business.PhoneNumber = request.PhoneNumber.Trim();
            if (request.ProvinceId.HasValue) business.ProvinceId = request.ProvinceId.Value;
            if (request.DistrictId.HasValue) business.DistrictId = request.DistrictId.Value;
            if (!string.IsNullOrWhiteSpace(request.LocationName)) business.LocationName = request.LocationName.Trim();
            if (request.Latitude.HasValue) business.Latitude = request.Latitude.Value;
            if (request.Longitude.HasValue) business.Longitude = request.Longitude.Value;
            await _context.SaveChangesAsync();
            return ServiceResponse<BusinessResponse>.SuccessResult(new BusinessResponse { Id = business.Id, Name = business.Name });
        }

        public async Task<ServiceResponse<bool>> DeleteBusinessAsync(Guid? userId, Guid businessId)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);
            if (business == null) return ServiceResponse<bool>.ErrorResult("İşletme bulunamadı");
            business.IsActive = false;
            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResult(true);
        }

        // BusinessService.cs içine sadece bu metodu yapıştırın (Eskisiyle değiştirin)

public async Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(CreateBusinessRequest request, Guid userId)
{
    // Transaction (İşlem bütünlüğü) başlatıyoruz
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return ServiceResponse<BusinessResponse>.ErrorResult("Kullanıcı bulunamadı.");
        }

      
        Random random = new Random();
        string verificationCode = random.Next(100000, 999999).ToString();

        
        var mainBusiness = new Personelim.Models.Business
        {
            Name = request.BusinessName,
            LocationName = string.Empty,
            Latitude = 0,
            Longitude = 0,
            PhoneNumber = request.PhoneNumber,
            ProvinceId = request.ProvinceId,
            DistrictId = request.DistrictId,
            Address = request.Address,
            Description = request.Description,
            OwnerId = userId,
            IsActive = false, 
            VerificationCode = verificationCode,
            SubBusinesses = new List<Personelim.Models.Business>(),
            CreatedAt = DateTime.UtcNow 
        };

        await _context.Businesses.AddAsync(mainBusiness);

        // Şubeler varsa ekle
        if (request.Offices != null && request.Offices.Count > 0)
        {
            foreach (var officeDto in request.Offices)
            {
                var subBusiness = new Personelim.Models.Business
                {
                    LocationName = officeDto.OfficeName,
                    Latitude = officeDto.Latitude,
                    Longitude = officeDto.Longitude,
                    Name = $"{request.BusinessName} - {officeDto.OfficeName}",
                    Address = mainBusiness.Address,
                    PhoneNumber = mainBusiness.PhoneNumber,
                    ProvinceId = mainBusiness.ProvinceId,
                    DistrictId = mainBusiness.DistrictId,
                    Description = mainBusiness.Description,
                    OwnerId = userId,
                    IsActive = false,
                    ParentBusiness = mainBusiness,
                    CreatedAt = DateTime.UtcNow
                };
                mainBusiness.SubBusinesses.Add(subBusiness);
            }
        }

        // Kullanıcıyı Owner olarak ata
        var ownerMembership = new Personelim.Models.BusinessMember
        {
            Business = mainBusiness,
            UserId = userId,
            Role = Personelim.Models.Enums.UserRole.Owner,
            Position = "İşletme Sahibi",
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _context.BusinessMembers.AddAsync(ownerMembership);
        await _context.SaveChangesAsync();

        // 3. Mail Gönderimi 
        // Eğer SMTP'de hata olursa catch'e düşecek veya false dönecek
        bool mailSent = await _emailService.SendBusinessVerificationCodeAsync(
            user.Email,
            user.FirstName,
            mainBusiness.Name,
            verificationCode
        );

        if (!mailSent)
        {
            // Mail gitmediyse hata fırlat ki aşağıdaki catch bloğu veritabanı işlemini geri alsın.
            throw new Exception("Doğrulama e-postası gönderilemedi. Lütfen e-posta adresinizi kontrol edin veya daha sonra tekrar deneyin.");
        }

        // Her şey yolundaysa işlemi onayla
        await transaction.CommitAsync();

        var responseDto = new BusinessResponse
        {
            Id = mainBusiness.Id,
            Name = mainBusiness.Name,
            PhoneNumber = mainBusiness.PhoneNumber,
            ProvinceId = mainBusiness.ProvinceId,
            DistrictId = mainBusiness.DistrictId,
            Address = mainBusiness.Address,
            Description = mainBusiness.Description,
            Latitude = mainBusiness.Latitude,
            Longitude = mainBusiness.Longitude,
            LocationName = mainBusiness.LocationName,
            CreatedAt = mainBusiness.CreatedAt,
        };

        return ServiceResponse<BusinessResponse>.SuccessResult(responseDto, "Şirket oluşturuldu. Lütfen e-postanıza gönderilen doğrulama kodunu giriniz.");
    }
    catch (Exception ex)
    {
        // Hata durumunda (Mail gitmezse veya DB hatası olursa) yapılan kayıtları geri al
        await transaction.RollbackAsync();

        var errorMessage = ex.Message;
        if (ex.InnerException != null) errorMessage += $" | DETAY: {ex.InnerException.Message}";
        
        return ServiceResponse<BusinessResponse>.ErrorResult("Şirket kaydedilemedi.", errorMessage);
    }
}


public async Task<ServiceResponse<bool>> VerifyBusinessAsync(Guid userId, VerifyBusinessRequest request)
{
    try
    {
        // Temizlik: Kodun başındaki/sonundaki boşlukları al
        string codeToCheck = request.Code?.Trim();
        if (string.IsNullOrEmpty(codeToCheck))
            return ServiceResponse<bool>.ErrorResult("Lütfen doğrulama kodunu giriniz.");

        // Doğrulanacak ana şirketi arıyoruz: OwnerId, VerificationCode ve IsVerified=false
        var business = await _context.Businesses
            .Include(b => b.SubBusinesses)
            .FirstOrDefaultAsync(b => 
                b.OwnerId == userId && 
                b.VerificationCode == codeToCheck && 
                b.IsVerified == false); 

        if (business == null)
            return ServiceResponse<bool>.ErrorResult("Geçersiz doğrulama kodu veya doğrulanacak işletme bulunamadı.");
        
        // 1. Ana İşletmeyi Doğrula ve Aktif Et
        business.IsVerified = true; 
        business.IsActive = true; // 🌟 EKLEME: İşletmeyi aktif hale getiriyoruz
        business.VerificationCode = null; 
        
        // 2. Alt İşletmeleri (Şubeleri) Doğrula ve Aktif Et
        if (business.SubBusinesses != null)
        {
            foreach (var sub in business.SubBusinesses)
            {
                sub.IsVerified = true; 
                sub.IsActive = true; // 🌟 EKLEME: Alt işletmeleri de aktif hale getiriyoruz
            }
        }

        await _context.SaveChangesAsync();
        return ServiceResponse<bool>.SuccessResult(true, "İşletmeniz başarıyla doğrulandı.");
    }
    catch (Exception ex)
    {
        // Loglama burada yapılabilir: _logger.LogError(ex, "Doğrulama hatası");
        return ServiceResponse<bool>.ErrorResult("Doğrulama hatası: " + ex.Message);
    }
}

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
        
        public Task<ServiceResponse<List<BusinessResponse>>> GetSubBusinessesAsync(Guid? userId, Guid parentBusinessId) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> UpdateSubBusinessAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId, UpdateBusinessRequest request) => throw new NotImplementedException();
        public Task<ServiceResponse<bool>> DeleteSubBusinessAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> GetSubBusinessByIdAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> CreateLocationAsync(Guid? userId, Guid parentBusinessId, CreateLocationRequest request) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> UpdateSubBusinessAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId, UpdateLocationRequest request) => throw new NotImplementedException();
    }
}