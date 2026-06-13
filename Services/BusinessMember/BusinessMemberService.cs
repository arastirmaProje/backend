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

                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId);
                bool isSubscribed = business?.IsSubscribed ?? false;

                var members = await _context.BusinessMembers
                    .Include(bm => bm.User)
                    .Include(bm => bm.Department)
                    .Where(bm => bm.BusinessId == businessId && bm.IsActive)
                    .ToListAsync();

                var result = members.Select(bm => MapToDto(bm, isSubscribed, business?.OwnerId)).ToList();
                return ServiceResponse<List<BusinessMemberResponseDto>>.SuccessResult(result);
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
                    .Include(bm => bm.Department)
                    .Include(bm => bm.Documents)
                    .FirstOrDefaultAsync(bm => bm.Id == memberId && bm.IsActive);
                if (member == null)
                    return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["PersonnelNotFound"]);

                var isMember = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == member.BusinessId && bm.IsActive);
                if (!isMember)
                    return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["NoPermissionViewPersonnel"]);

                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == member.BusinessId);
                bool isSubscribed = business?.IsSubscribed ?? false;

                return ServiceResponse<BusinessMemberResponseDto>.SuccessResult(MapToDto(member, isSubscribed, business?.OwnerId));
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
                    .Include(bm => bm.Department)
                    .FirstOrDefaultAsync(bm => bm.Id == memberId && bm.IsActive);
                if (targetMember == null)
                    return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["PersonnelNotFound"]);

                var requester = await _context.BusinessMembers
                    .FirstOrDefaultAsync(bm => bm.UserId == currentUserId && bm.BusinessId == targetMember.BusinessId && bm.IsActive);

                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == targetMember.BusinessId);
                bool isSubscribed = business?.IsSubscribed ?? false;

                var requesterRole = JobTitles.GetRole(requester?.Position);
                var targetRole    = JobTitles.GetRole(targetMember.Position);

                var allowedPositions = JobTitles.EffectiveManagementPositions(isSubscribed);
                if (requester == null || !allowedPositions.Contains(requester.Position))
                    return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["NoPermissionUpdatePersonnel"]);

                if (requestDto.PositionId.HasValue)
                {
                    if (!isSubscribed)
                        return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["SubscriptionRequired"]);

                    if (!JobTitles.IsValidId(requestDto.PositionId.Value))
                        return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["InvalidJobTitle"]);

                    if (targetMember.UserId != currentUserId && targetRole >= requesterRole)
                        return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["CannotEditHigherOrEqualRole"]);

                    if (JobTitles.GetRoleById(requestDto.PositionId.Value) >= requesterRole)
                        return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["CannotAssignHigherRole"]);

                    targetMember.Position = JobTitles.GetTitleName(requestDto.PositionId.Value)!;
                }

                if (requestDto.DepartmentId.HasValue)
                {
                    if (!isSubscribed)
                        return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["SubscriptionRequired"]);

                    var deptExists = await _context.Departments
                        .AnyAsync(d => d.Id == requestDto.DepartmentId && d.BusinessId == targetMember.BusinessId && d.IsActive);
                    if (!deptExists)
                        return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["GeneralError"]);

                    targetMember.DepartmentId = requestDto.DepartmentId;
                }

                if (!string.IsNullOrEmpty(requestDto.TCIdentityNumber))
                {
                    if (requestDto.TCIdentityNumber.Length != 11 || !requestDto.TCIdentityNumber.All(char.IsDigit))
                        return ServiceResponse<BusinessMemberResponseDto>.ErrorResult(_localizer["TCInvalid"]);
                }

                if (requestDto.Salary.HasValue)          targetMember.Salary           = requestDto.Salary;
                if (requestDto.TCIdentityNumber != null)  targetMember.TCIdentityNumber = requestDto.TCIdentityNumber;
                targetMember.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ServiceResponse<BusinessMemberResponseDto>.SuccessResult(MapToDto(targetMember, isSubscribed, business?.OwnerId), _localizer["PersonnelUpdated"]);
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
                    return ServiceResponse<bool>.ErrorResult(_localizer["CannotRemoveSelf"]);

                var requester = await _context.BusinessMembers
                    .FirstOrDefaultAsync(bm => bm.UserId == currentUserId && bm.BusinessId == targetMember.BusinessId && bm.IsActive);

                var businessForRemove = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == targetMember.BusinessId);
                bool isSubscribedForRemove = businessForRemove?.IsSubscribed ?? false;

                var requesterRole = JobTitles.GetRole(requester?.Position);
                var targetRole    = JobTitles.GetRole(targetMember.Position);

                var allowedForRemove = JobTitles.EffectiveManagementPositions(isSubscribedForRemove);
                if (requester == null || !allowedForRemove.Contains(requester.Position))
                    return ServiceResponse<bool>.ErrorResult(_localizer["NoPermissionRemovePersonnel"]);

                if (isSubscribedForRemove && targetRole >= requesterRole)
                    return ServiceResponse<bool>.ErrorResult(_localizer["CannotEditHigherOrEqualRole"]);

                targetMember.IsActive  = false;
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
                string fullPath       = Path.Combine(uploadFolder, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                    await requestDto.File.CopyToAsync(stream);

                string dbFilePath = Path.Combine("uploads", "documents", member.BusinessId.ToString(), member.Id.ToString(), uniqueFileName).Replace("\\", "/");
                var document = new MemberDocument
                {
                    BusinessMemberId = memberId,
                    DocumentType     = requestDto.DocumentType,
                    FileName         = requestDto.File.FileName,
                    FilePath         = dbFilePath,
                    FileExtension    = ext,
                    UploadedAt       = DateTime.UtcNow
                };
                _context.MemberDocuments.Add(document);
                await _context.SaveChangesAsync();

                return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.SuccessResult(new BusinessMemberResponseDto.MemberDocumentResponse
                {
                    Id           = document.Id,
                    DocumentType = document.DocumentType,
                    FileName     = document.FileName,
                    FileUrl      = document.FilePath,
                    UploadedAt   = document.UploadedAt
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

                var docBusiness = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == doc.BusinessMember.BusinessId);
                var docUpdateAllowed = JobTitles.EffectiveManagementPositions(docBusiness?.IsSubscribed ?? false);
                var canUpdateDoc = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == doc.BusinessMember.BusinessId && docUpdateAllowed.Contains(bm.Position) && bm.IsActive);
                if (!canUpdateDoc) return ServiceResponse<BusinessMemberResponseDto.MemberDocumentResponse>.ErrorResult(_localizer["NoPermissionUpdateDocument"]);

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
                    string newFullPath    = Path.Combine(uploadFolder, uniqueFileName);
                    using (var stream = new FileStream(newFullPath, FileMode.Create)) await requestDto.File.CopyToAsync(stream);

                    doc.FileName      = requestDto.File.FileName;
                    doc.FilePath      = Path.Combine("uploads", "documents", doc.BusinessMember.BusinessId.ToString(), doc.BusinessMember.Id.ToString(), uniqueFileName).Replace("\\", "/");
                    doc.FileExtension = ext;
                    doc.UploadedAt    = DateTime.UtcNow;
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
            var downloadBusiness = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == doc.BusinessMember.BusinessId);
            var downloadAllowed = JobTitles.EffectiveManagementPositions(downloadBusiness?.IsSubscribed ?? false);
            bool isManager = await _context.BusinessMembers
                .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == doc.BusinessMember.BusinessId && downloadAllowed.Contains(bm.Position) && bm.IsActive);

            if (!isSelf && !isManager)
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

                var deleteBusiness = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == doc.BusinessMember.BusinessId);
                var deleteDocAllowed = JobTitles.EffectiveManagementPositions(deleteBusiness?.IsSubscribed ?? false);
                var canDeleteDoc = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == doc.BusinessMember.BusinessId && deleteDocAllowed.Contains(bm.Position) && bm.IsActive);
                if (!canDeleteDoc) return ServiceResponse<bool>.ErrorResult(_localizer["NoPermissionDeleteDocument"]);

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
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == requestDto.BusinessId);
                bool isSubscribed = business?.IsSubscribed ?? false;

                var allowedToAdd = JobTitles.EffectiveManagementPositions(isSubscribed);
                var canAdd = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == requestDto.BusinessId && allowedToAdd.Contains(bm.Position) && bm.IsActive);
                if (!canAdd) return ServiceResponse<Guid>.ErrorResult(_localizer["NoPermissionAddPersonnel"]);

                string positionToSave;
                Guid? departmentIdToSave = null;

                if (isSubscribed)
                {
                    if (!JobTitles.IsValidId(requestDto.PositionId))
                        return ServiceResponse<Guid>.ErrorResult(_localizer["InvalidJobTitle"]);

                    positionToSave = JobTitles.GetTitleName(requestDto.PositionId)!;

                    if (requestDto.DepartmentId.HasValue)
                    {
                        var deptExists = await _context.Departments
                            .AnyAsync(d => d.Id == requestDto.DepartmentId && d.BusinessId == requestDto.BusinessId && d.IsActive);
                        if (!deptExists)
                            return ServiceResponse<Guid>.ErrorResult(_localizer["GeneralError"]);

                        departmentIdToSave = requestDto.DepartmentId;
                    }
                }
                else
                {
                    positionToSave = "Diğer";
                }

                var businessName = business?.Name;
                var user         = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == requestDto.Email.ToLower());

                string temporaryPassword = null;
                bool   isNewUser         = false;
                if (user == null)
                {
                    isNewUser        = true;
                    temporaryPassword = GenerateRandomPassword();
                    user = new User { Id = Guid.NewGuid(), Email = requestDto.Email.Trim(), FirstName = requestDto.FirstName.Trim(), LastName = requestDto.LastName.Trim(), PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    await _context.Users.AddAsync(user);
                }
                else
                {
                    if (await _context.BusinessMembers.AnyAsync(bm => bm.UserId == user.Id && bm.BusinessId == requestDto.BusinessId && bm.IsActive))
                        return ServiceResponse<Guid>.ErrorResult(_localizer["AlreadyMember"]);
                }

                var newMember = new Models.BusinessMember { BusinessId = requestDto.BusinessId, UserId = user.Id, Position = positionToSave, DepartmentId = departmentIdToSave, Salary = requestDto.Salary, TCIdentityNumber = requestDto.TCIdentityNumber, JoinedAt = DateTime.UtcNow, IsActive = true };
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

        private static BusinessMemberResponseDto MapToDto(Models.BusinessMember bm, bool isSubscribed, Guid? businessOwnerId = null)
        {
            var isOwner = businessOwnerId.HasValue && bm.UserId == businessOwnerId.Value;
            var show    = isSubscribed || isOwner;
            return new()
            {
            Id               = bm.Id,
            UserId           = bm.UserId,
            FullName         = bm.User != null ? bm.User.FirstName + " " + bm.User.LastName : string.Empty,
            Email            = bm.User?.Email ?? string.Empty,
            PositionId       = show ? JobTitles.GetTitleId(bm.Position) : 0,
            PositionName     = show ? bm.Position : "Diğer",
            Role             = show ? JobTitles.GetRole(bm.Position).ToString() : UserRole.Employee.ToString(),
            DepartmentId     = show ? bm.DepartmentId : null,
            DepartmentName   = show ? bm.Department?.Category : null,
            Salary           = bm.Salary,
            TCIdentityNumber = bm.TCIdentityNumber,
            JoinedAt         = bm.JoinedAt,
            IsActive         = bm.IsActive,
            Documents        = bm.Documents?.Select(d => new BusinessMemberResponseDto.MemberDocumentResponse
            {
                Id           = d.Id,
                DocumentType = d.DocumentType,
                FileName     = d.FileName,
                FileUrl      = d.FilePath,
                UploadedAt   = d.UploadedAt
            }).ToList() ?? new()
            };
        }

        private static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
