using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Personelim.Data;
using Personelim.DTOs.Business;
using Personelim.Helpers;
using Personelim.Services.Email;
using Personelim.Models; 

namespace Personelim.Services.Business
{
    public class BusinessService : IBusinessService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<BusinessService> _logger;
        private readonly IWebHostEnvironment _env; 

        public BusinessService(AppDbContext context, IEmailService emailService, ILogger<BusinessService> logger, IWebHostEnvironment env)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _env = env;
        }

      
        public async Task<ServiceResponse<List<BusinessResponse>>> GetUserBusinessesAsync(Guid? userId)
        {
            if (!userId.HasValue)
                return ServiceResponse<List<BusinessResponse>>.SuccessResult(new List<BusinessResponse>());

            var businesses = await _context.Businesses
                .Include(b => b.Province)
                .Include(b => b.District)
                .Include(b => b.Members)
                .Include(b => b.ParentBusiness)
                .Include(b => b.SubBusinesses)
                .Where(b => b.OwnerId == userId && b.IsActive && b.ParentBusinessId == null)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var responseList = businesses
                .Select(b => MapToBusinessResponse(b, userId))
                .ToList();

            return ServiceResponse<List<BusinessResponse>>.SuccessResult(responseList);
        }
        
        public async Task<ServiceResponse<BusinessResponse>> GetBusinessByIdAsync(Guid? userId, Guid businessId)
        {
            var business = await _context.Businesses
                .Include(b => b.Province)
                .Include(b => b.District)
                .Include(b => b.ParentBusiness)
                .Include(b => b.SubBusinesses)
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);

            if (business == null) 
                return ServiceResponse<BusinessResponse>.ErrorResult("İşletme bulunamadı");

            var response = MapToBusinessResponse(business, userId);
            return ServiceResponse<BusinessResponse>.SuccessResult(response);
        }

        // =========================================================================
        // 3. UPDATE BUSINESS
        // =========================================================================
        public async Task<ServiceResponse<BusinessResponse>> UpdateBusinessAsync(Guid userId, Guid businessId, UpdateBusinessRequest request)
        {
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);
            
            if (business == null) 
                return ServiceResponse<BusinessResponse>.ErrorResult("İşletme bulunamadı.");

            if (business.OwnerId != userId)
                return ServiceResponse<BusinessResponse>.ErrorResult("Bu işlem için yetkiniz yok.");

            // Metin Alanları
            if (!string.IsNullOrWhiteSpace(request.Name)) business.Name = request.Name.Trim();
            if (!string.IsNullOrWhiteSpace(request.Description)) business.Description = request.Description.Trim();
            if (!string.IsNullOrWhiteSpace(request.Address)) business.Address = request.Address.Trim();
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber)) business.PhoneNumber = request.PhoneNumber.Trim();
            
            if (request.ProvinceId.HasValue) business.ProvinceId = request.ProvinceId.Value;
            if (request.DistrictId.HasValue) business.DistrictId = request.DistrictId.Value;
            
            if (!string.IsNullOrWhiteSpace(request.LocationName)) business.LocationName = request.LocationName.Trim();
            if (request.Latitude.HasValue) business.Latitude = request.Latitude.Value;
            if (request.Longitude.HasValue) business.Longitude = request.Longitude.Value;

            // Resim Yükleme
            if (request.Image != null && request.Image.Length > 0)
            {
                try
                {
                    var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "business-logos");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    var fileExtension = Path.GetExtension(request.Image.FileName);
                    var fileName = $"{business.Id}_{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.Image.CopyToAsync(stream);
                    }

                    business.ImageUrl = $"/uploads/business-logos/{fileName}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Resim yükleme sırasında hata oluştu.");
                }
            }

            business.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Güncel veriyi ilişkilerle tekrar çek
            var updatedBusiness = await _context.Businesses
                .Include(b => b.Province)
                .Include(b => b.District)
                .Include(b => b.ParentBusiness)
                .Include(b => b.SubBusinesses)
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == businessId);

            var response = MapToBusinessResponse(updatedBusiness!, userId);
            return ServiceResponse<BusinessResponse>.SuccessResult(response, "İşletme başarıyla güncellendi.");
        }

        // =========================================================================
        // 4. CREATE BUSINESS
        // =========================================================================
        public async Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(CreateBusinessRequest request, Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return ServiceResponse<BusinessResponse>.ErrorResult("Kullanıcı bulunamadı.");
            
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
                
                bool mailSent = await _emailService.SendBusinessVerificationCodeAsync(
                    user.Email,
                    user.FirstName,
                    mainBusiness.Name,
                    verificationCode
                );
                
                if (!mailSent) throw new Exception("Doğrulama e-postası gönderilemedi.");
                
                await transaction.CommitAsync();

                var createdBusiness = await _context.Businesses
                     .Include(b => b.Province)
                     .Include(b => b.District)
                     .Include(b => b.Members)
                     .Include(b => b.SubBusinesses)
                     .FirstOrDefaultAsync(b => b.Id == mainBusiness.Id);

                var response = MapToBusinessResponse(createdBusiness!, userId);
                return ServiceResponse<BusinessResponse>.SuccessResult(response, "Şirket oluşturuldu. Doğrulama kodu gönderildi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResponse<BusinessResponse>.ErrorResult("Şirket kaydedilemedi.", ex.Message);
            }
        }

        // =========================================================================
        // 5. VERIFY BUSINESS
        // =========================================================================
        public async Task<ServiceResponse<bool>> VerifyBusinessAsync(Guid userId, VerifyBusinessRequest request)
        {
            try
            {
                string codeToCheck = request.Code?.Trim();
                if (string.IsNullOrEmpty(codeToCheck))
                    return ServiceResponse<bool>.ErrorResult("Lütfen doğrulama kodunu giriniz.");
                
                var business = await _context.Businesses
                    .Include(b => b.SubBusinesses)
                    .FirstOrDefaultAsync(b => 
                        b.OwnerId == userId && 
                        b.VerificationCode == codeToCheck && 
                        b.IsVerified == false); 
                
                if (business == null)
                    return ServiceResponse<bool>.ErrorResult("Geçersiz kod veya işletme bulunamadı.");
                
                business.IsVerified = true; 
                business.IsActive = true;
                business.VerificationCode = null; 
                
                if (business.SubBusinesses != null)
                {
                    foreach (var sub in business.SubBusinesses)
                    {
                        sub.IsVerified = true; 
                        sub.IsActive = true;
                    }
                }
                await _context.SaveChangesAsync();
                return ServiceResponse<bool>.SuccessResult(true, "İşletmeniz başarıyla doğrulandı.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult("Doğrulama hatası: " + ex.Message);
            }
        }

        // =========================================================================
        // HELPER MAPPER
        // =========================================================================
        private BusinessResponse MapToBusinessResponse(Personelim.Models.Business business, Guid? currentUserId)
        {
            string userRole = "Visitor";
            if (currentUserId.HasValue && business.Members != null)
            {
                var member = business.Members.FirstOrDefault(m => m.UserId == currentUserId);
                if (member != null) userRole = member.Role.ToString();
            }

            return new BusinessResponse
            {
                Id = business.Id,
                Name = business.Name,
                Description = business.Description,
                Address = business.Address,
                PhoneNumber = business.PhoneNumber,
                ImageUrl = business.ImageUrl,
                LocationName = business.LocationName,
                Latitude = business.Latitude,
                Longitude = business.Longitude,
                ProvinceId = business.ProvinceId,
                ProvinceName = business.Province?.Name ?? string.Empty,
                DistrictId = business.DistrictId,
                DistrictName = business.District?.Name ?? string.Empty,
                Role = userRole,
                MemberCount = business.Members?.Count ?? 0,
                ParentBusinessId = business.ParentBusinessId,
                ParentBusinessName = business.ParentBusiness?.Name,
                IsSubBusiness = business.ParentBusinessId != null,
                SubBusinessCount = business.SubBusinesses?.Count ?? 0,
                CreatedAt = business.CreatedAt
            };
        }

        // Henüz kullanılmayan metodlar
        public Task<ServiceResponse<List<BusinessResponse>>> GetSubBusinessesAsync(Guid? userId, Guid parentBusinessId) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> UpdateSubBusinessAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId, UpdateBusinessRequest request) => throw new NotImplementedException();
        public Task<ServiceResponse<bool>> DeleteBusinessAsync(Guid? userId, Guid businessId) => throw new NotImplementedException();
        public Task<ServiceResponse<bool>> DeleteSubBusinessAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> GetSubBusinessByIdAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> CreateLocationAsync(Guid? userId, Guid parentBusinessId, CreateLocationRequest request) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> UpdateSubBusinessAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId, UpdateLocationRequest request) => throw new NotImplementedException();
    }
}