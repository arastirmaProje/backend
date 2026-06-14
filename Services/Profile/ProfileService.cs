using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Personelim.Data;
using Personelim.DTOs.Auth;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using Personelim.Resources;

namespace Personelim.Services.Auth
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ProfileService(AppDbContext context, IWebHostEnvironment env, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _env = env;
            _localizer = localizer;
        }

        public async Task<ServiceResponse<UserProfileResponseDto>> GetUserProfileAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.BusinessMemberships).ThenInclude(bm => bm.Business)
                    .Include(u => u.OwnedBusinesses)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null) return ServiceResponse<UserProfileResponseDto>.ErrorResult(_localizer["UserNotFound"]);

                var response = new UserProfileResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ImageUrl = user.ImageUrl,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    BusinessCount = user.BusinessMemberships.Count(bm => bm.IsActive),
                    OwnedBusinessCount = user.OwnedBusinesses.Count(b => b.IsActive),
                    Memberships = user.BusinessMemberships
                        .Where(bm => bm.IsActive)
                        .Select(bm =>
                        {
                            var sub     = bm.Business?.IsSubscribed ?? false;
                            var isOwner = bm.Business?.OwnerId == userId;
                            var show    = sub || isOwner;
                            return new UserMembershipDto
                            {
                                BusinessMemberId = bm.Id,
                                BusinessId       = bm.BusinessId,
                                BusinessName     = bm.Business?.Name ?? string.Empty,
                                Role             = show ? JobTitles.GetRole(bm.Position).ToString() : UserRole.Employee.ToString(),
                                PositionId       = show ? JobTitles.GetTitleId(bm.Position) : 0,
                                PositionName     = show ? bm.Position : "Diğer"
                            };
                        }).ToList()
                };
                return ServiceResponse<UserProfileResponseDto>.SuccessResult(response);
            }
            catch (Exception ex) 
            { 
                return ServiceResponse<UserProfileResponseDto>.ErrorResult(_localizer["GeneralError"], ex.Message); 
            }
        }

        public async Task<ServiceResponse<UserProfileResponseDto>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequestDto requestDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return ServiceResponse<UserProfileResponseDto>.ErrorResult(_localizer["UserNotFound"]);

                if (!string.IsNullOrWhiteSpace(requestDto.Email) && requestDto.Email.ToLower() != user.Email)
                {
                    if (await _context.Users.AnyAsync(u => u.Email == requestDto.Email.ToLower() && u.Id != userId && u.IsActive))
                        return ServiceResponse<UserProfileResponseDto>.ErrorResult(_localizer["EmailAlreadyInUse"]);
                    
                    user.Email = requestDto.Email.ToLower();
                }

                if (!string.IsNullOrWhiteSpace(requestDto.FirstName)) user.FirstName = requestDto.FirstName;
                if (!string.IsNullOrWhiteSpace(requestDto.LastName)) user.LastName = requestDto.LastName;

                if (requestDto.Image != null && requestDto.Image.Length > 0)
                {
                    var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "user-avatars");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                    var extension = Path.GetExtension(requestDto.Image?.FileName ?? ".jpg");
                    var fileName = $"{user.Id}_{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await requestDto.Image.CopyToAsync(stream);
                    }
                    user.ImageUrl = $"/uploads/user-avatars/{fileName}";
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return await GetUserProfileAsync(userId);
            }
            catch (Exception ex) 
            { 
                return ServiceResponse<UserProfileResponseDto>.ErrorResult(_localizer["ProfileUpdateError"], ex.Message); 
            }
        }

        public async Task<ServiceResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequestDto requestDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return ServiceResponse<bool>.ErrorResult(_localizer["UserNotFound"]);

                if (!BCrypt.Net.BCrypt.Verify(requestDto.CurrentPassword, user.PasswordHash))
                    return ServiceResponse<bool>.ErrorResult(_localizer["CurrentPasswordWrong"]);

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResult(true, _localizer["PasswordChangedSuccessfully"]);
            }
            catch (Exception ex) { return ServiceResponse<bool>.ErrorResult(ex.Message); }
        }

        public async Task<ServiceResponse<bool>> DeleteUserAsync(Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.Include(u => u.OwnedBusinesses).FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return ServiceResponse<bool>.ErrorResult(_localizer["UserNotFound"]);
                
                var ownedBusinesses = await _context.Businesses.Where(b => b.OwnerId == userId).ToListAsync();
                ownedBusinesses.ForEach(b => b.IsActive = false);

                var memberships = await _context.BusinessMembers.Where(bm => bm.UserId == userId).ToListAsync();
                memberships.ForEach(m => m.IsActive = false);

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResponse<bool>.SuccessResult(true, _localizer["AccountDeleted"]);
            }
            catch (Exception ex) 
            { 
                await transaction.RollbackAsync(); 
                return ServiceResponse<bool>.ErrorResult(_localizer["GeneralError"], ex.Message); 
            }
        }
    }
}