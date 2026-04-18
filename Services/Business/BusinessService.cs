using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Personelim.Data;
using Personelim.DTOs.Business;
using Personelim.Helpers;
using Personelim.Services.Email;
using Personelim.Models; 
using Personelim.Resources;

namespace Personelim.Services.Business
{
    public class BusinessService : IBusinessService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<BusinessService> _logger;
        private readonly IWebHostEnvironment _env; 
        private readonly IStringLocalizer<SharedResource> _localizer;

        public BusinessService(
            AppDbContext context, 
            IEmailService emailService, 
            ILogger<BusinessService> logger, 
            IWebHostEnvironment env,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _env = env;
            _localizer = localizer;
        }
        
        public async Task<ServiceResponse<List<BusinessResponseDto>>> GetAllBusinessesAsync(Guid? userId)
        {
            if (!userId.HasValue)
                return ServiceResponse<List<BusinessResponseDto>>.SuccessResult(new List<BusinessResponseDto>());
            try
            {
                var businesses = await _context.Businesses
                    .Include(b => b.Province)
                    .Include(b => b.District)
                    .Include(b => b.Members)        
                    .Include(b => b.ParentBusiness)
                    .Include(b => b.SubBusinesses)  
                    .Where(b => 
                        b.ParentBusinessId == null && 
                        (
                            b.OwnerId == userId 
                            || 
                            (b.IsActive && b.Members.Any(m => m.UserId == userId && m.IsActive))
                        )
                    )
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
      
                var responseList = businesses
                    .Select(b => MapToBusinessResponse(b, userId)) 
                    .ToList();
                return ServiceResponse<List<BusinessResponseDto>>.SuccessResult(responseList, _localizer["BusinessesListed"]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _localizer["ErrorListingBusinesses"]);
                return ServiceResponse<List<BusinessResponseDto>>.ErrorResult(_localizer["GeneralError"]);
            }
        }
      
        public async Task<ServiceResponse<BusinessResponseDto>> GetBusinessByIdAsync(Guid? userId, Guid businessId)
        {
            var business = await _context.Businesses
                .Include(b => b.Province)
                .Include(b => b.District)
                .Include(b => b.ParentBusiness)
                .Include(b => b.SubBusinesses)
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);
            
            if (business == null) 
                return ServiceResponse<BusinessResponseDto>.ErrorResult(_localizer["BusinessNotFound"]);
            
            var response = MapToBusinessResponse(business, userId);
            return ServiceResponse<BusinessResponseDto>.SuccessResult(response);
        }
        
        public async Task<ServiceResponse<BusinessResponseDto>> UpdateBusinessAsync(Guid userId, Guid businessId, UpdateBusinessRequestDto requestDto)
        {
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);
            
            if (business == null) 
                return ServiceResponse<BusinessResponseDto>.ErrorResult(_localizer["BusinessNotFound"]);
            if (business.OwnerId != userId)
                return ServiceResponse<BusinessResponseDto>.ErrorResult(_localizer["UnauthorizedAction"]);
            
            if (!string.IsNullOrWhiteSpace(requestDto.Name)) business.Name = requestDto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(requestDto.Description)) business.Description = requestDto.Description.Trim();
            if (!string.IsNullOrWhiteSpace(requestDto.Address)) business.Address = requestDto.Address.Trim();
            if (!string.IsNullOrWhiteSpace(requestDto.PhoneNumber)) business.PhoneNumber = requestDto.PhoneNumber.Trim();
            
            if (requestDto.ProvinceId.HasValue) business.ProvinceId = requestDto.ProvinceId.Value;
            if (requestDto.DistrictId.HasValue) business.DistrictId = requestDto.DistrictId.Value;
            
            if (!string.IsNullOrWhiteSpace(requestDto.LocationName)) business.LocationName = requestDto.LocationName.Trim();
            if (requestDto.Latitude.HasValue) business.Latitude = requestDto.Latitude.Value;
            if (requestDto.Longitude.HasValue) business.Longitude = requestDto.Longitude.Value;
            
            if (requestDto.Image != null && requestDto.Image.Length > 0)
            {
                try
                {
                    var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "business-logos");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                    var fileExtension = Path.GetExtension(requestDto.Image.FileName);
                    var fileName = $"{business.Id}_{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await requestDto.Image.CopyToAsync(stream);
                    }
                    business.ImageUrl = $"/uploads/business-logos/{fileName}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, _localizer["ImageUploadError"]);
                }
            }
            business.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            var updatedBusiness = await _context.Businesses
                .Include(b => b.Province)
                .Include(b => b.District)
                .Include(b => b.ParentBusiness)
                .Include(b => b.SubBusinesses)
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == businessId);

            if (updatedBusiness == null)
                return ServiceResponse<BusinessResponseDto>.ErrorResult(_localizer["BusinessNotFound"]);

            var response = MapToBusinessResponse(updatedBusiness, userId);
            return ServiceResponse<BusinessResponseDto>.SuccessResult(response, _localizer["BusinessUpdated"]);
        }
        
        public async Task<ServiceResponse<BusinessResponseDto>> CreateBusinessAsync(CreateBusinessRequestDto requestDto, Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return ServiceResponse<BusinessResponseDto>.ErrorResult(_localizer["UserNotFound"]);
            
                Random random = new Random();
                string verificationCode = random.Next(100000, 999999).ToString();
                
                var mainBusiness = new Personelim.Models.Business
                {
                    Name = requestDto.BusinessName,
                    LocationName = string.Empty,
                    Latitude = 0,
                    Longitude = 0,
                    PhoneNumber = requestDto.PhoneNumber,
                    ProvinceId = requestDto.ProvinceId,
                    DistrictId = requestDto.DistrictId,
                    Address = requestDto.Address,
                    Description = requestDto.Description,
                    OwnerId = userId,
                    IsActive = false, 
                    VerificationCode = verificationCode,
                    SubBusinesses = new List<Personelim.Models.Business>(),
                    CreatedAt = DateTime.UtcNow 
                };
                await _context.Businesses.AddAsync(mainBusiness);
                
                if (requestDto.Offices != null && requestDto.Offices.Count > 0)
                {
                    foreach (var officeDto in requestDto.Offices)
                    {
                        var subBusiness = new Personelim.Models.Business
                        {
                            LocationName = officeDto.OfficeName,
                            Latitude = officeDto.Latitude,
                            Longitude = officeDto.Longitude,
                            Name = $"{requestDto.BusinessName} - {officeDto.OfficeName}",
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
                
                if (!mailSent)
                    _logger.LogWarning("Email gönderilemedi. Doğrulama kodu: {Code} — Email: {Email}", verificationCode, user.Email);
                
                await transaction.CommitAsync();
                
                var createdBusiness = await _context.Businesses
                     .Include(b => b.Province)
                     .Include(b => b.District)
                     .Include(b => b.Members)
                     .Include(b => b.SubBusinesses)
                     .FirstOrDefaultAsync(b => b.Id == mainBusiness.Id);

                if (createdBusiness == null)
                    return ServiceResponse<BusinessResponseDto>.ErrorResult(_localizer["BusinessNotFound"]);

                var response = MapToBusinessResponse(createdBusiness, userId);
                return ServiceResponse<BusinessResponseDto>.SuccessResult(response, _localizer["BusinessCreatedVerificationSent"]);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResponse<BusinessResponseDto>.ErrorResult(_localizer["BusinessVerificationError"], ex.Message);
            }
        }
        
        public async Task<ServiceResponse<bool>> VerifyBusinessAsync(Guid userId, VerifyBusinessRequestDto requestDto)
        {
            try
            {
                string codeToCheck = requestDto.Code?.Trim();
                if (string.IsNullOrEmpty(codeToCheck))
                    return ServiceResponse<bool>.ErrorResult(_localizer["EnterVerificationCode"]);
                
                var business = await _context.Businesses
                    .Include(b => b.SubBusinesses)
                    .FirstOrDefaultAsync(b => 
                        b.OwnerId == userId && 
                        b.VerificationCode == codeToCheck && 
                        b.IsVerified == false); 
                
                if (business == null)
                    return ServiceResponse<bool>.ErrorResult(_localizer["InvalidCodeOrBusiness"]);
                
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
                return ServiceResponse<bool>.SuccessResult(true, _localizer["BusinessVerified"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult(_localizer["VerificationError"] + ex.Message);
            }
        }
        
        private BusinessResponseDto MapToBusinessResponse(Personelim.Models.Business business, Guid? currentUserId)
        {
            return new BusinessResponseDto
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
                MemberCount = business.Members?.Count ?? 0,
                ParentBusinessId = business.ParentBusinessId,
                ParentBusinessName = business.ParentBusiness?.Name,
                IsSubBusiness = business.ParentBusinessId != null,
                SubBusinessCount = business.SubBusinesses?.Count ?? 0,
                IsSubscribed = business.IsSubscribed,
                CreatedAt = business.CreatedAt
            };
        }

        public async Task<ServiceResponse<bool>> SubscribeAsync(Guid userId, Guid businessId)
        {
            try
            {
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);
                if (business == null)
                    return ServiceResponse<bool>.ErrorResult(_localizer["BusinessNotFound"]);

                if (business.OwnerId != userId)
                    return ServiceResponse<bool>.ErrorResult(_localizer["UnauthorizedAction"]);

                if (business.IsSubscribed)
                    return ServiceResponse<bool>.SuccessResult(true);

                business.IsSubscribed = true;
                business.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<bool>> UnsubscribeAsync(Guid userId, Guid businessId)
        {
            try
            {
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);
                if (business == null)
                    return ServiceResponse<bool>.ErrorResult(_localizer["BusinessNotFound"]);

                if (business.OwnerId != userId)
                    return ServiceResponse<bool>.ErrorResult(_localizer["UnauthorizedAction"]);

                business.IsSubscribed = false;
                business.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }
        
        public async Task<ServiceResponse<BusinessDocumentResponseDto>> UploadBusinessDocumentAsync(
            Guid userId, Guid businessId, UploadBusinessDocumentRequestDto requestDto)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);
            if (business == null) return ServiceResponse<BusinessDocumentResponseDto>.ErrorResult(_localizer["BusinessNotFound"]);
            if (business.OwnerId != userId) return ServiceResponse<BusinessDocumentResponseDto>.ErrorResult(_localizer["UnauthorizedAction"]);
            if (requestDto.File == null || requestDto.File.Length == 0)
                return ServiceResponse<BusinessDocumentResponseDto>.ErrorResult(_localizer["FileNotFound"]);
            
            var ext = Path.GetExtension(requestDto.File.FileName).ToLowerInvariant();
            var allowed = new HashSet<string> { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" };
            if (!allowed.Contains(ext))
                return ServiceResponse<BusinessDocumentResponseDto>.ErrorResult(_localizer["InvalidFileExtension"]);
            
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadFolder = Path.Combine(webRoot, "uploads", "business-documents", businessId.ToString());
            Directory.CreateDirectory(uploadFolder);
            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadFolder, uniqueName);
            
            using (var stream = new FileStream(fullPath, FileMode.Create))
                await requestDto.File.CopyToAsync(stream);
            
            var dbPath = Path.Combine("uploads", "business-documents", businessId.ToString(), uniqueName)
                .Replace("\\", "/");
            
            var doc = new BusinessDocument
            {
                BusinessId = businessId,
                DocumentType = requestDto.DocumentType,
                FileName = requestDto.File.FileName,
                FilePath = dbPath,
                FileExtension = ext,
                UploadedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.BusinessDocuments.Add(doc);
            await _context.SaveChangesAsync();
            
            return ServiceResponse<BusinessDocumentResponseDto>.SuccessResult(new BusinessDocumentResponseDto
            {
                Id = doc.Id,
                DocumentType = doc.DocumentType,
                FileName = doc.FileName,
                FileUrl = "/" + doc.FilePath,  
                UploadedAt = doc.UploadedAt
            }, _localizer["DocumentUploaded"]);
        }

        public async Task<ServiceResponse<List<BusinessDocumentResponseDto>>> GetBusinessDocumentsAsync(Guid userId, Guid businessId)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);
            if (business == null) return ServiceResponse<List<BusinessDocumentResponseDto>>.ErrorResult(_localizer["BusinessNotFound"]);
            
            var docs = await _context.BusinessDocuments
                .Where(d => d.BusinessId == businessId && d.IsActive)
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new BusinessDocumentResponseDto
                {
                    Id = d.Id,
                    DocumentType = d.DocumentType,
                    FileName = d.FileName,
                    FileUrl = "/" + d.FilePath,
                    UploadedAt = d.UploadedAt
                })
                .ToListAsync();
            return ServiceResponse<List<BusinessDocumentResponseDto>>.SuccessResult(docs);
        }

        public async Task<ServiceResponse<bool>> DeleteBusinessDocumentAsync(Guid userId, Guid documentId)
        {
            var doc = await _context.BusinessDocuments
                .Include(d => d.Business)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.IsActive);
            
            if (doc == null) return ServiceResponse<bool>.ErrorResult(_localizer["DocumentNotFound"]);
            if (doc.Business.OwnerId != userId) return ServiceResponse<bool>.ErrorResult(_localizer["UnauthorizedAction"]);
            
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(webRoot, doc.FilePath);
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            
            doc.IsActive = false;
            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResult(true, _localizer["DocumentDeleted"]);
        }
    }
}