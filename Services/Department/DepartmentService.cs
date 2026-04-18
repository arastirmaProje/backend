using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Personelim.Data;
using Personelim.DTOs.Department;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using Personelim.Resources;

namespace Personelim.Services.Department
{
    public class DepartmentService : IDepartmentService
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public DepartmentService(AppDbContext context, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<ServiceResponse<List<DepartmentResponseDto>>> GetDepartmentsByBusinessIdAsync(Guid currentUserId, Guid businessId)
        {
            try
            {
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive);
                if (business == null)
                    return ServiceResponse<List<DepartmentResponseDto>>.ErrorResult(_localizer["BusinessNotFound"]);

                if (!business.IsSubscribed)
                    return ServiceResponse<List<DepartmentResponseDto>>.ErrorResult(_localizer["SubscriptionRequired"]);

                var isMember = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == businessId && bm.IsActive);
                if (!isMember)
                    return ServiceResponse<List<DepartmentResponseDto>>.ErrorResult(_localizer["NoPermissionViewPersonnel"]);

                var departments = await _context.Departments
                    .Where(d => d.BusinessId == businessId && d.IsActive)
                    .Select(d => new { d.Id, d.BusinessId, d.Category, MemberCount = d.Members.Count(m => m.IsActive), d.CreatedAt })
                    .OrderBy(d => d.Category)
                    .ToListAsync();

                var result = departments.Select(d => new DepartmentResponseDto
                {
                    Id = d.Id,
                    BusinessId = d.BusinessId,
                    CategoryId = JobTitles.GetCategoryId(d.Category),
                    Name = d.Category,
                    MemberCount = d.MemberCount,
                    CreatedAt = d.CreatedAt
                }).ToList();

                return ServiceResponse<List<DepartmentResponseDto>>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<DepartmentResponseDto>>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<DepartmentResponseDto>> CreateDepartmentAsync(Guid currentUserId, CreateDepartmentRequestDto requestDto)
        {
            try
            {
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == requestDto.BusinessId && b.IsActive);
                if (business == null)
                    return ServiceResponse<DepartmentResponseDto>.ErrorResult(_localizer["BusinessNotFound"]);

                if (!business.IsSubscribed)
                    return ServiceResponse<DepartmentResponseDto>.ErrorResult(_localizer["SubscriptionRequired"]);

                var allowedPositions = JobTitles.EffectiveManagementPositions(true);
                var canManage = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == requestDto.BusinessId && allowedPositions.Contains(bm.Position) && bm.IsActive);
                if (!canManage)
                    return ServiceResponse<DepartmentResponseDto>.ErrorResult(_localizer["NoPermissionAddPersonnel"]);

                var categoryName = JobTitles.GetCategoryName(requestDto.CategoryId);
                if (categoryName == null)
                    return ServiceResponse<DepartmentResponseDto>.ErrorResult(_localizer["GeneralError"]);

                var duplicate = await _context.Departments
                    .AnyAsync(d => d.BusinessId == requestDto.BusinessId && d.Category == categoryName && d.IsActive);
                if (duplicate)
                    return ServiceResponse<DepartmentResponseDto>.ErrorResult(_localizer["GeneralError"]);

                var department = new Models.Department
                {
                    BusinessId = requestDto.BusinessId,
                    Category = categoryName
                };
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                return ServiceResponse<DepartmentResponseDto>.SuccessResult(new DepartmentResponseDto
                {
                    Id = department.Id,
                    BusinessId = department.BusinessId,
                    CategoryId = requestDto.CategoryId,
                    Name = categoryName,
                    MemberCount = 0,
                    CreatedAt = department.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return ServiceResponse<DepartmentResponseDto>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<DepartmentResponseDto>> UpdateDepartmentAsync(Guid currentUserId, Guid departmentId, UpdateDepartmentRequestDto requestDto)
        {
            try
            {
                var department = await _context.Departments
                    .Include(d => d.Business)
                    .FirstOrDefaultAsync(d => d.Id == departmentId && d.IsActive);
                if (department == null)
                    return ServiceResponse<DepartmentResponseDto>.ErrorResult(_localizer["GeneralError"]);

                if (!department.Business!.IsSubscribed)
                    return ServiceResponse<DepartmentResponseDto>.ErrorResult(_localizer["SubscriptionRequired"]);

                var allowedPositions = JobTitles.EffectiveManagementPositions(true);
                var canManage = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == department.BusinessId && allowedPositions.Contains(bm.Position) && bm.IsActive);
                if (!canManage)
                    return ServiceResponse<DepartmentResponseDto>.ErrorResult(_localizer["NoPermissionUpdatePersonnel"]);

                if (requestDto.CategoryId.HasValue)
                {
                    var newCategoryName = JobTitles.GetCategoryName(requestDto.CategoryId.Value);
                    if (newCategoryName == null)
                        return ServiceResponse<DepartmentResponseDto>.ErrorResult(_localizer["GeneralError"]);
                    department.Category = newCategoryName;
                }

                await _context.SaveChangesAsync();

                var memberCount = await _context.BusinessMembers.CountAsync(bm => bm.DepartmentId == departmentId && bm.IsActive);

                return ServiceResponse<DepartmentResponseDto>.SuccessResult(new DepartmentResponseDto
                {
                    Id = department.Id,
                    BusinessId = department.BusinessId,
                    CategoryId = JobTitles.GetCategoryId(department.Category),
                    Name = department.Category,
                    MemberCount = memberCount,
                    CreatedAt = department.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return ServiceResponse<DepartmentResponseDto>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<bool>> DeleteDepartmentAsync(Guid currentUserId, Guid departmentId)
        {
            try
            {
                var department = await _context.Departments
                    .Include(d => d.Business)
                    .FirstOrDefaultAsync(d => d.Id == departmentId && d.IsActive);
                if (department == null)
                    return ServiceResponse<bool>.ErrorResult(_localizer["GeneralError"]);

                if (!department.Business!.IsSubscribed)
                    return ServiceResponse<bool>.ErrorResult(_localizer["SubscriptionRequired"]);

                var allowedPositions = JobTitles.EffectiveManagementPositions(true);
                var canManage = await _context.BusinessMembers
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == department.BusinessId && allowedPositions.Contains(bm.Position) && bm.IsActive);
                if (!canManage)
                    return ServiceResponse<bool>.ErrorResult(_localizer["NoPermissionRemovePersonnel"]);

                var membersInDept = await _context.BusinessMembers
                    .Where(bm => bm.DepartmentId == departmentId)
                    .ToListAsync();
                foreach (var m in membersInDept)
                    m.DepartmentId = null;

                department.IsActive = false;
                await _context.SaveChangesAsync();

                return ServiceResponse<bool>.SuccessResult(true);
            }
            catch (Exception ex)
            {
                return ServiceResponse<bool>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }
    }
}
