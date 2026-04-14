using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Personelim.Data;
using Personelim.DTOs.Invitation;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using Personelim.Services.Email;
using Personelim.Resources;
using BCrypt.Net;

namespace Personelim.Services.Invitation
{
    public class InvitationService : IInvitationService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public InvitationService(
            AppDbContext context, 
            IEmailService emailService,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _emailService = emailService;
            _localizer = localizer;
        }

        public async Task<ServiceResponse<InvitationResponseDto>> SendInvitationAsync(Guid userId, SendInvitationRequestDto requestDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var business = await _context.Businesses.FindAsync(requestDto.BusinessId);
                if (business == null)
                    return ServiceResponse<InvitationResponseDto>.ErrorResult(_localizer["BusinessNotFound"]);

                var inviter = await _context.Users.FindAsync(userId);
                if (inviter == null)
                    return ServiceResponse<InvitationResponseDto>.ErrorResult(_localizer["UserNotFound"]);

                var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == requestDto.Email.ToLower());
                
                bool isNewUser = false;
                string generatedPassword = null; 

                if (targetUser == null)
                {
                    isNewUser = true;
                    generatedPassword = GenerateRandomPassword(); 
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(generatedPassword);
                    string nameFromEmail = requestDto.Email.Split('@')[0];
                    
                    targetUser = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = requestDto.Email.ToLower(),
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
                    var isMember = await _context.BusinessMembers.AnyAsync(bm =>
                        bm.UserId == targetUser.Id &&
                        bm.BusinessId == requestDto.BusinessId &&
                        bm.IsActive);
                    if (isMember)
                    {
                        return ServiceResponse<InvitationResponseDto>.ErrorResult(_localizer["AlreadyPersonnel"]);
                    }
                }
                
                var member = new Models.BusinessMember
                {
                    BusinessId = requestDto.BusinessId,
                    UserId = targetUser.Id,
                    Role = UserRole.Employee,
                    Position = _localizer["PersonnelRole"],
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };
                await _context.BusinessMembers.AddAsync(member);
                
                var logEntry = new Models.Invitation
                {
                    BusinessId = business.Id,
                    Email = requestDto.Email,
                    InvitedByUserId = userId,
                    Status = InvitationStatus.Accepted, 
                    Message = requestDto.Message ?? _localizer["DirectlyAdded"],
                    InvitationCode = "DIRECT-" + Guid.NewGuid().ToString().Substring(0,6),
                    CreatedAt = DateTime.UtcNow,
                    AcceptedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
                _context.Invitations.Add(logEntry);
                await _context.SaveChangesAsync();
                
                bool mailSent = false;
                if (isNewUser)
                {
                    mailSent = await _emailService.SendAccountCreatedEmailAsync(
                        targetUser.Email, 
                        targetUser.FirstName, 
                        generatedPassword, 
                        business.Name
                    );
                }
                else
                {
                    mailSent = await _emailService.SendAddedToBusinessEmailAsync(
                        targetUser.Email, 
                        targetUser.FirstName, 
                        business.Name
                    );
                }

                await transaction.CommitAsync();

                string msg = isNewUser 
                    ? _localizer["NewUserAddedComplete"] 
                    : _localizer["ExistingUserAddedComplete"];

                if (!mailSent) msg += _localizer["MailSentErrorSuffix"];
                
                return ServiceResponse<InvitationResponseDto>.SuccessResult(new InvitationResponseDto 
                {
                    Id = logEntry.Id,
                    Email = targetUser.Email,
                    Message = _localizer["ProcessSuccessful"]
                }, msg);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ServiceResponse<InvitationResponseDto>.ErrorResult(_localizer["GeneralError"] + ": " + ex.Message);
            }
        }
        
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
        }
       
        public Task<ServiceResponse<string>> AcceptInvitationAsync(Guid userId, string code) => throw new NotImplementedException();
        public Task<ServiceResponse<string>> CancelInvitationAsync(Guid userId, Guid id) => throw new NotImplementedException();
        public async Task<ServiceResponse<List<InvitationResponseDto>>> GetUserInvitationsAsync(string email) 
        {
            return ServiceResponse<List<InvitationResponseDto>>.SuccessResult(new List<InvitationResponseDto>());
        }
    }
}