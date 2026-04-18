using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Personelim.Data;
using Personelim.DTOs.Auth;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Services.Email;
using Personelim.Resources; // Resource namespace
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Personelim.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AuthService(
            AppDbContext context, 
            IConfiguration configuration,
            IEmailService emailService,
            IWebHostEnvironment env,
            IStringLocalizer<SharedResource> localizer) 
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _env = env;
            _localizer = localizer;
        }

        public async Task<ServiceResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto requestDto)
        {
            try
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == requestDto.Email.ToLower());
                if (emailExists)
                {
                    return ServiceResponse<AuthResponseDto>.ErrorResult(_localizer["EmailAlreadyExists"]);
                }
                
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.Password);
                
                var newUser = new User
                {
                    FirstName = requestDto.firstName,
                    LastName = requestDto.lastName,
                    Email = requestDto.Email.ToLower(), 
                    PasswordHash = passwordHash,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    ImageUrl = null
                };
                
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                
                var token = GenerateJwtToken(newUser);
                var response = new AuthResponseDto
                {
                    UserId = newUser.Id,
                    Email = newUser.Email,
                    FirstName = newUser.FirstName,
                    LastName = newUser.LastName,
                    Role ="Owner" ,
                    ImageUrl = newUser.ImageUrl,
                    Token = token.Token,
                    ExpiresAt = token.ExpiresAt
                };
                return ServiceResponse<AuthResponseDto>.SuccessResult(response, _localizer["UserRegisteredSuccessfully"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<AuthResponseDto>.ErrorResult(_localizer["ErrorDuringRegistration"], ex.Message);
            }
        }

        public async Task<ServiceResponse<AuthResponseDto>> LoginAsync(LoginRequestDto requestDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == requestDto.Email.ToLower());
                
                if (user == null || !user.IsActive)
                {
                    return ServiceResponse<AuthResponseDto>.ErrorResult(_localizer["InvalidCredentials"]);
                }
                
                if (!BCrypt.Net.BCrypt.Verify(requestDto.Password, user.PasswordHash))
                {
                    return ServiceResponse<AuthResponseDto>.ErrorResult(_localizer["InvalidCredentials"]);
                }
                
                var membership = await _context.BusinessMembers
                    .FirstOrDefaultAsync(bm => bm.UserId == user.Id && bm.IsActive);
                
                string userRole = membership != null
                    ? Personelim.Helpers.JobTitles.GetRole(membership.Position).ToString()
                    : "Owner";
                
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                var token = GenerateJwtToken(user);
                
                var response = new AuthResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = userRole, 
                    ImageUrl = user.ImageUrl, 
                    Token = token.Token,
                    ExpiresAt = token.ExpiresAt
                };
                return ServiceResponse<AuthResponseDto>.SuccessResult(response, _localizer["LoginSuccessful"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<AuthResponseDto>.ErrorResult(_localizer["ErrorDuringLogin"], ex.Message);
            }
        }
       
        public async Task<ServiceResponse<bool>> LogoutAsync(Guid userId)
        {
            return ServiceResponse<bool>.SuccessResult(true, _localizer["LogoutSuccessful"]);
        }
       
        private (string Token, DateTime ExpiresAt) GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var expiresAt = DateTime.UtcNow.AddDays(7);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.GetFullName())
                }),
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature ?? SecurityAlgorithms.HmacSha256),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return (tokenHandler.WriteToken(token), expiresAt);
        }

        public async Task<ServiceResponse<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive);
                if (user == null)
                {
                    return ServiceResponse<ForgotPasswordResponse>.SuccessResult(
                        new ForgotPasswordResponse { Email = request.Email, ExpiresAt = DateTime.UtcNow.AddMinutes(15), ExpiresInMinutes = 15 },
                        _localizer["PasswordResetEmailIfExist"]
                    );
                }
                
                var oldTokens = await _context.PasswordResetTokens
                    .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();
                foreach (var token in oldTokens) { token.IsUsed = true; token.UsedAt = DateTime.UtcNow; }
                
                var code = GenerateRandomCode();
                var expiresAt = DateTime.UtcNow.AddMinutes(15);
                var resetToken = new PasswordResetToken { UserId = user.Id, Code = code, ExpiresAt = expiresAt };
                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();
                
                var emailSent = await _emailService.SendPasswordResetCodeAsync(user.Email, code, user.GetFullName());
                if (!emailSent) return ServiceResponse<ForgotPasswordResponse>.ErrorResult(_localizer["EmailCouldNotBeSent"]);
                
                var response = new ForgotPasswordResponse { Email = user.Email, ExpiresAt = expiresAt, ExpiresInMinutes = 15 };
                return ServiceResponse<ForgotPasswordResponse>.SuccessResult(response, _localizer["PasswordResetCodeSent"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<ForgotPasswordResponse>.ErrorResult(ex.Message);
            }
        }

        public async Task<ServiceResponse<bool>> VerifyResetCodeAsync(VerifyResetCodeRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive);
                if (user == null) return ServiceResponse<bool>.ErrorResult(_localizer["InvalidCode"]);
                
                var token = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.UserId == user.Id && t.Code == request.Code && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);
                if (token == null) return ServiceResponse<bool>.ErrorResult(_localizer["InvalidCode"]);
                
                return ServiceResponse<bool>.SuccessResult(true, _localizer["CodeVerified"]);
            }
            catch (Exception ex) { return ServiceResponse<bool>.ErrorResult(ex.Message); }
        }

        public async Task<ServiceResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive);
                if (user == null) return ServiceResponse<bool>.ErrorResult(_localizer["InvalidProcess"]);
                
                var token = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.UserId == user.Id && t.Code == request.Code && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);
                if (token == null) return ServiceResponse<bool>.ErrorResult(_localizer["InvalidCode"]);
                
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return ServiceResponse<bool>.SuccessResult(true, _localizer["PasswordChangedSuccessfully"]);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResponse<bool>.ErrorResult(ex.Message);
            }
        }

        private string GenerateRandomCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}