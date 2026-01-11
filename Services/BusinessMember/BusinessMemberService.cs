using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Personelim.Data;
using Personelim.DTOs.BusinessMember;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using Personelim.Services.Email;
using Personelim.Resources;

namespace Personelim.Services.BusinessMember
{
    public class BusinessMemberService : IBusinessMemberService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env; 
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public BusinessMemberService(
            AppDbContext context, 
            IWebHostEnvironment env, 
            IEmailService emailService,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _env = env;
            _emailService = emailService;
            _localizer = localizer;
        }
        
        public async Task<ServiceResponse<List<BusinessMemberResponseDto>>> GetMembersByBusinessIdAsync(Guid currentUserId, Guid businessId)
        {
            try
            {
                var isMember = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == businessId && bm.IsActive);
                if (!isMember)
                    return ServiceResponse<List<BusinessMemberResponseDto>>.ErrorResult(_localizer["NoPermissionViewPersonnel"]);
                
                var members = await _context.BusinessMembers
                    .Include(bm => bm.User)
                    .Where(bm => bm.BusinessId == businessId && bm.IsActive)
                    .Select(bm => new BusinessMemberResponseDto
                    {
                        Id = bm.Id,
                        UserId = bm.UserId,
                        FullName = bm.User.FirstName + " " + bm.User.LastName,
                        Email = bm.User.Email,
                        Role = bm.Role.ToString(),
                        Position = bm.Position,
                        Salary = bm.Salary,                 
                        TCIdentityNumber = bm.TCIdentityNumber, 
                        JoinedAt = bm.JoinedAt,
                        IsActive = bm.IsActive
                    })
                    .ToListAsync();
                return ServiceResponse<List<BusinessMemberResponseDto>>.SuccessResult(members);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<BusinessMemberResponseDto>>.ErrorResult(_localizer["ErrorPersonnelList"], ex.Message);
            }
        }
        
        public async Task<ServiceResponse<BusinessMemberResponseDto>> GetMemberByIdAsync(Guid currentUserId, Guid memberId)
        {
            try
            {
                var member = await _context.BusinessMembers
                    .Include(bm => bm.User)
                    .Include(bm => bm.Documents) 
                    .FirstOrDefaultAsync(bm => bm.Id == memberId && bm.IsActive);
                if (member == null)
                    return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["PersonnelNotFound"]);
                
                var requester = await _context.BusinessMembers
                    .FirstOrDefaultAsync(bm => bm.UserId == currentUserId && bm.BusinessId == member.BusinessId && bm.IsActive);
                if (requester == null)
                    return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["NoPermissionViewPersonnel"]);
                
                var response = new BusinessMemberResponseDto
                {
                    Id = member.Id,
                    UserId = member.UserId,
                    FullName = member.User.FirstName + " " + member.User.LastName,
                    Email = member.User.Email,
                    Role = member.Role.ToString(),
                    Position = member.Position,
                    Salary = member.Salary,                
                    TCIdentityNumber = member.TCIdentityNumber, 
                    JoinedAt = member.JoinedAt,
                    IsActive = member.IsActive,
                    Documents = member.Documents.Select(d => new BusinessMemberResponseDto.MemberDocumentResponse
                    {
                        Id = d.Id,
                        DocumentType = d.DocumentType,
                        FileName = d.FileName,
                        FileUrl = d.FilePath,
                        UploadedAt = d.UploadedAt
                    }).ToList()
                };
                return ServiceResponse<BusinessMemberResponseDto>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["ErrorPersonnelDetail"], ex.Message);
            }
        }
        
        public async Task<ServiceResponse<BusinessMemberResponseDto>> UpdateMemberAsync(Guid currentUserId, Guid memberId, UpdateBusinessMemberRequestDto requestDto)
        {
            try
            {
                var targetMember = await _context.BusinessMembers
                    .Include(bm => bm.User)
                    .FirstOrDefaultAsync(bm => bm.Id == memberId && bm.IsActive);
                if (targetMember == null)
                    return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["PersonnelNotFound"]);
                
                if (!string.IsNullOrEmpty(requestDto.TCIdentityNumber))
                {
                    if (requestDto.TCIdentityNumber.Length != 11 || !requestDto.TCIdentityNumber.All(char.IsDigit))
                    {
                        return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["TCInvalid"]);
                    }
                }
                
                targetMember.Role = requestDto.Role;
                targetMember.Position = requestDto.Position;
                targetMember.Salary = requestDto.Salary;                 
                targetMember.TCIdentityNumber = requestDto.TCIdentityNumber; 
                targetMember.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                var response = new BusinessMemberResponseDto
                {
                    Id = targetMember.Id,
                    UserId = targetMember.UserId,
                    FullName = targetMember.User.FirstName + " " + targetMember.User.LastName,
                    Email = targetMember.User.Email,
                    Role = targetMember.Role.ToString(),
                    Position = targetMember.Position,
                    Salary = targetMember.Salary,
                    TCIdentityNumber = targetMember.TCIdentityNumber,
                    JoinedAt = targetMember.JoinedAt,
                    IsActive = targetMember.IsActive
                };
                return ServiceResponse<BusinessMemberResponseDto>.SuccessResult(response, _localizer["PersonnelUpdated"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["ErrorUpdatePersonnel"], ex.Message);
            }
        }
        
        public async Task<ServiceResponse<bool>> RemoveMemberAsync(Guid currentUserId, Guid memberId)
        {
            try
            {
                var targetMember = await _context.BusinessMembers
                    .FirstOrDefaultAsync(bm => bm.Id == memberId && bm.IsActive);
                if (targetMember == null)
                    return ServiceResponse<bool>.ErrorResult(_localizer["PersonnelNotFound"]);
                
                if (targetMember.UserId == currentUserId)
                {
                    return ServiceResponse<bool>.ErrorResult(_localizer["CannotRemoveSelf"]);
                }
                
                var requester = await _context.BusinessMembers
                    .FirstOrDefaultAsync(bm => bm.UserId == currentUserId && bm.BusinessId == targetMember.BusinessId && bm.IsActive);
                if (requester == null || requester.Role != UserRole.Owner)
                {
                     return ServiceResponse<bool>.ErrorResult(_localizer["NoPermissionRemovePersonnel"]);
                }
                
                targetMember.IsActive = false;
                targetMember.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return ServiceResponse<bool>.SuccessResult(true, _localizer["PersonnelRemoved"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult(_localizer["ErrorRemovePersonnel"], ex.Message);
            }
        }
        
        public async Task<ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>> UploadDocumentAsync(Guid currentUserId, Guid memberId, UploadDocumentRequestDto requestDto)
        {
            try
            {
                var member = await _context.BusinessMembers.FirstOrDefaultAsync(bm => bm.Id == memberId && bm.IsActive);
                if (member == null) return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.ErrorResult(_localizer["PersonnelNotFound"]);
                
                if (requestDto.File == null || requestDto.File.Length == 0)
                    return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.ErrorResult(_localizer["FileNotFound"]);
                
                var ext = Path.GetExtension(requestDto.File.FileName).ToLower();
                if (ext != ".pdf")
                    return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.ErrorResult(_localizer["OnlyPdfAllowed"]);
               
                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "documents", member.BusinessId.ToString(), member.Id.ToString());
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                
                string uniqueFileName = Guid.NewGuid().ToString() + ext;
                string fullPath = Path.Combine(uploadFolder, uniqueFileName);
                
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await requestDto.File.CopyToAsync(stream);
                }
                
                string dbFilePath = Path.Combine("uploads", "documents", member.BusinessId.ToString(), member.Id.ToString(), uniqueFileName).Replace("\\", "/");
                var document = new MemberDocument
                {
                    BusinessMemberId = memberId,
                    DocumentType = requestDto.DocumentType,
                    FileName = requestDto.File.FileName, 
                    FilePath = dbFilePath,
                    FileExtension = ext,
                    UploadedAt = DateTime.UtcNow
                };
                _context.MemberDocuments.Add(document);
                await _context.SaveChangesAsync();
                
                return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.SuccessResult(new BusinessMemberResponseDto.MemberDocumentResponse
                {
                    Id = document.Id,
                    DocumentType = document.DocumentType,
                    FileName = document.FileName,
                    FileUrl = document.FilePath,
                    UploadedAt = document.UploadedAt
                }, _localizer["DocumentUploadSuccess"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.ErrorResult(_localizer["ErrorDocumentUpload"] + ": " + ex.Message);
            }
        }
        
        public async Task<ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>> UpdateDocumentAsync(Guid currentUserId, Guid documentId, UpdateDocumentRequestDto requestDto)
        {
            try
            {
                var doc = await _context.MemberDocuments
                    .Include(d => d.BusinessMember)
                    .FirstOrDefaultAsync(d => d.Id == documentId);
                if (doc == null) return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.ErrorResult(_localizer["DocumentNotFound"]);
                
                var isOwner = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == doc.BusinessMember.BusinessId && bm.Role == UserRole.Owner && bm.IsActive);
                if (!isOwner) return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.ErrorResult(_localizer["NoPermissionUpdateDocument"]);
                
                if (!string.IsNullOrWhiteSpace(requestDto.DocumentType)) doc.DocumentType = requestDto.DocumentType;
                
                if (requestDto.File != null && requestDto.File.Length > 0)
                {
                    var ext = Path.GetExtension(requestDto.File.FileName).ToLower();
                    if (ext != ".pdf")
                        return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.ErrorResult(_localizer["OnlyPdfAllowed"]);
                    
                    string oldFullPath = Path.Combine(_env.WebRootPath, doc.FilePath);
                    if (System.IO.File.Exists(oldFullPath)) System.IO.File.Delete(oldFullPath);
                    
                    string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "documents", doc.BusinessMember.BusinessId.ToString(), doc.BusinessMember.Id.ToString());
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                    
                    string uniqueFileName = Guid.NewGuid().ToString() + ext;
                    string newFullPath = Path.Combine(uploadFolder, uniqueFileName);
                    using (var stream = new FileStream(newFullPath, FileMode.Create)) await requestDto.File.CopyToAsync(stream);
                    
                    doc.FileName = requestDto.File.FileName; 
                    doc.FilePath = Path.Combine("uploads", "documents", doc.BusinessMember.BusinessId.ToString(), doc.BusinessMember.Id.ToString(), uniqueFileName).Replace("\\", "/");
                    doc.FileExtension = ext;
                    doc.UploadedAt = DateTime.UtcNow; 
                }
                
                await _context.SaveChangesAsync();
                return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.SuccessResult(new BusinessMemberResponseDto.MemberDocumentResponse
                {
                    Id = doc.Id, DocumentType = doc.DocumentType, FileName = doc.FileName, FileUrl = doc.FilePath, UploadedAt = doc.UploadedAt
                }, _localizer["DocumentUpdateSuccess"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.ErrorResult(_localizer["ErrorDocumentUpdate"] + ": " + ex.Message);
            }
        }

        public async Task<ServiceResponse<DocumentDownloadResponseDto>> GetDocumentFileAsync(Guid currentUserId, Guid documentId)
        {
            var doc = await _context.MemberDocuments.Include(d => d.BusinessMember).FirstOrDefaultAsync(d => d.Id == documentId);
            if (doc == null) return ServiceResponse<DocumentDownloadResponseDto>.ErrorResult(_localizer["DocumentNotFound"]);

            bool isSelf = doc.BusinessMember.UserId == currentUserId;
            bool isOwner = await _context.BusinessMembers.AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == doc.BusinessMember.BusinessId && bm.Role == UserRole.Owner && bm.IsActive);
            
            if (!isSelf && !isOwner)
                return ServiceResponse<DocumentDownloadResponseDto>.ErrorResult(_localizer["NoPermissionViewDocument"]);
            
            string fullPath = Path.Combine(_env.WebRootPath, doc.FilePath);
            if (!System.IO.File.Exists(fullPath))
                return ServiceResponse<DocumentDownloadResponseDto>.ErrorResult(_localizer["FileNotFoundOnServer"]);
            
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return ServiceResponse<DocumentDownloadResponseDto>.SuccessResult(new DocumentDownloadResponseDto { FileBytes = fileBytes, FileName = doc.FileName, ContentType = "application/pdf" });
        }
       
        public async Task<ServiceResponse<bool>> DeleteDocumentAsync(Guid currentUserId, Guid documentId)
        {
            try
            {
                var doc = await _context.MemberDocuments.Include(d => d.BusinessMember).FirstOrDefaultAsync(d => d.Id == documentId);
                if (doc == null) return ServiceResponse<bool>.ErrorResult(_localizer["DocumentNotFound"]);
                
                var isOwner = await _context.BusinessMembers.AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == doc.BusinessMember.BusinessId && bm.Role == UserRole.Owner && bm.IsActive);
                if (!isOwner) return ServiceResponse<bool>.ErrorResult(_localizer["NoPermissionDeleteDocument"]);
               
                string fullPath = Path.Combine(_env.WebRootPath, doc.FilePath);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                
                _context.MemberDocuments.Remove(doc);
                await _context.SaveChangesAsync();
                return ServiceResponse<bool>.SuccessResult(true, _localizer["DocumentDeleteSuccess"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult(_localizer["ErrorDocumentDelete"] + ": " + ex.Message);
            }
        }

        public async Task<ServiceResponse<Guid>> AddEmployeeDirectlyAsync(Guid currentUserId, AddEmployeeRequestDto requestDto)
        {
             using var transaction = await _context.Database.BeginTransactionAsync();
             try
             {
                 var isOwner = await _context.BusinessMembers.AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == requestDto.BusinessId && bm.Role == UserRole.Owner && bm.IsActive);
                 if (!isOwner) return ServiceResponse<Guid>.ErrorResult(_localizer["NoPermissionAddPersonnel"]);
                 
                 var businessName = await _context.Businesses.Where(b => b.Id == requestDto.BusinessId).Select(b => b.Name).FirstOrDefaultAsync();
                 var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == requestDto.Email.ToLower());
                 
                 string temporaryPassword = null;
                 bool isNewUser = false;
                 if (user == null)
                 {
                     isNewUser = true;
                     temporaryPassword = GenerateRandomPassword();
                     user = new User { Id = Guid.NewGuid(), Email = requestDto.Email.Trim(), FirstName = requestDto.FirstName.Trim(), LastName = requestDto.LastName.Trim(), PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                     await _context.Users.AddAsync(user);
                 }
                 else
                 {
                     if (await _context.BusinessMembers.AnyAsync(bm => bm.UserId == user.Id && bm.BusinessId == requestDto.BusinessId && bm.IsActive))
                         return ServiceResponse<Guid>.ErrorResult(_localizer["AlreadyMember"]);
                 }
                 
                 if (!Enum.TryParse<UserRole>(requestDto.Role, true, out var roleEnum)) roleEnum = UserRole.Employee;
                 var newMember = new Models.BusinessMember { BusinessId = requestDto.BusinessId, UserId = user.Id, Role = roleEnum, Position = requestDto.Position, Salary = requestDto.Salary, TCIdentityNumber = requestDto.TCIdentityNumber, JoinedAt = DateTime.UtcNow, IsActive = true };
                 await _context.BusinessMembers.AddAsync(newMember);
                 await _context.SaveChangesAsync();
              
                 bool mailSent = isNewUser 
                     ? await _emailService.SendAccountCreatedEmailAsync(user.Email, user.FirstName, temporaryPassword, businessName)
                     : await _emailService.SendAddedToBusinessEmailAsync(user.Email, user.FirstName, businessName);

                 await transaction.CommitAsync();

                 string msg = isNewUser 
                     ? (mailSent ? _localizer["AccountCreatedMailSent"] : string.Format(_localizer["MailNotSent"], temporaryPassword))
                     : _localizer["ExistingUserAdded"];

                 return ServiceResponse<Guid>.SuccessResult(newMember.Id, msg);
             }
             catch (Exception ex)
             {
                 await transaction.RollbackAsync();
                 return ServiceResponse<Guid>.ErrorResult(_localizer["GeneralError"], ex.Message);
             }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}