using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Business;
using Personelim.Helpers;

namespace Personelim.Services.Business
{
    public class BusinessService : IBusinessService
    {
        private readonly AppDbContext _context;

        public BusinessService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResponse<BusinessResponse>> CreateBusinessAsync(
    Guid? userId, // nullable
    CreateBusinessRequest request,
    Guid? parentBusinessId = null)
{
    if (!userId.HasValue)
        return ServiceResponse<BusinessResponse>.ErrorResult("Kullanıcı bilgisi bulunamadı.");

    if (string.IsNullOrWhiteSpace(request.BusinessName))
        return ServiceResponse<BusinessResponse>.ErrorResult("İşletme adı boş olamaz.");
    if (string.IsNullOrWhiteSpace(request.Address))
        return ServiceResponse<BusinessResponse>.ErrorResult("Adres boş olamaz.");
    if (request.ProvinceId <= 0 || request.DistrictId <= 0)
        return ServiceResponse<BusinessResponse>.ErrorResult("Geçersiz il veya ilçe bilgisi.");

    var province = await _context.Provinces.FindAsync(request.ProvinceId);
    if (province == null)
        return ServiceResponse<BusinessResponse>.ErrorResult("Belirtilen il bulunamadı.");

    var district = await _context.Districts
        .FirstOrDefaultAsync(d => d.Id == request.DistrictId && d.ProvinceId == request.ProvinceId);
    if (district == null)
        return ServiceResponse<BusinessResponse>.ErrorResult("Belirtilen ilçe bulunamadı veya il ile eşleşmiyor.");

    var business = new Models.Business
    {
        Id = Guid.NewGuid(),
        Name = request.BusinessName.Trim(),
        Description = request.description?.Trim(),
        Address = request.Address.Trim(),
        PhoneNumber = request.PhoneNumber?.Trim(),
        ProvinceId = province.Id,
        DistrictId = district.Id,
        OwnerId = userId.Value, // nullable -> .Value
        ParentBusinessId = parentBusinessId,
        LocationName = request.OfficeName?.Trim(),
        Latitude = request.BusinessLatitude ,
        Longitude = request.BusinessLongitude ,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    _context.Businesses.Add(business);
    await _context.SaveChangesAsync();

    return ServiceResponse<BusinessResponse>.SuccessResult(new BusinessResponse
    {
        Id = business.Id,
        Name = business.Name
    });
}



        public async Task<ServiceResponse<List<BusinessResponse>>> GetUserBusinessesAsync(Guid? userId)
        {
            if (!userId.HasValue)
                return ServiceResponse<List<BusinessResponse>>.SuccessResult(new List<BusinessResponse>());

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
        public async Task<ServiceResponse<BusinessResponse>> LoginAndCreateBusinessAsync(LoginAndCreateBusinessRequest request)
        {
            // 1) Login
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null || !user.IsActive)
                return ServiceResponse<BusinessResponse>.ErrorResult("Email veya şifre hatalı");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return ServiceResponse<BusinessResponse>.ErrorResult("Email veya şifre hatalı");

            // 2) Şirket oluştur
            var business = new Models.Business
            {
                Name = request.BusinessName,
                PhoneNumber = request.PhoneNumber,
                ProvinceId = request.ProvinceId,
                DistrictId = request.DistrictId,
                Address = request.Address,
                Description = request.Description,
                OwnerId = user.Id,
                Latitude = request.BusinessLatitude,
                Longitude = request.BusinessLongitude,
                LocationName = request.LocationName,
                
            };

            await _context.Businesses.AddAsync(business);
            await _context.SaveChangesAsync();


            var responseDto = new BusinessResponse
            {
                Id = business.Id,
                Name = business.Name,
                PhoneNumber = business.PhoneNumber,
                ProvinceId = business.ProvinceId,
                DistrictId = business.DistrictId,
                Address = business.Address,
                Description = business.Description,
                LocationName = business.LocationName,
                Latitude = business.Latitude,
                Longitude = business.Longitude,
                
                CreatedAt = business.CreatedAt
            };

            return ServiceResponse<BusinessResponse>.SuccessResult(responseDto, "Şirket başarıyla oluşturuldu");

        }


// Şifre hash fonksiyonu
        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }


        // Diğer SubBusiness metotları burada aynı mantıkla eklenebilir
        public Task<ServiceResponse<List<BusinessResponse>>> GetSubBusinessesAsync(Guid? userId, Guid parentBusinessId) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> UpdateSubBusinessAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId, UpdateBusinessRequest request) => throw new NotImplementedException();
        public Task<ServiceResponse<bool>> DeleteSubBusinessAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> GetSubBusinessByIdAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> CreateLocationAsync(Guid? userId, Guid parentBusinessId, CreateLocationRequest request) => throw new NotImplementedException();
        public Task<ServiceResponse<BusinessResponse>> UpdateSubBusinessAsync(Guid? userId, Guid parentBusinessId, Guid subBusinessId, UpdateLocationRequest request) => throw new NotImplementedException();
    }
    
}
