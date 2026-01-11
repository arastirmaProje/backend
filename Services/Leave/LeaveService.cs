using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Personelim.Data;
using Personelim.DTOs.Leave;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using Personelim.Resources;

namespace Personelim.Services.Leave
{
    public class LeaveService : ILeaveService
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public LeaveService(AppDbContext context, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<ServiceResponse<LeaveResponseDto>> CreateLeaveRequestAsync(Guid userId, CreateLeaveRequestDto requestDto)
        {
            if (requestDto.StartDate > requestDto.EndDate)
                return ServiceResponse<LeaveResponseDto>.ErrorResult(_localizer["StartDateAfterEndDate"]);
           
            var member = await _context.BusinessMembers
                .Include(bm => bm.User)
                .FirstOrDefaultAsync(bm => bm.UserId == userId && bm.BusinessId == requestDto.BusinessId && bm.IsActive);
            
            if (member == null)
                return ServiceResponse<LeaveResponseDto>.ErrorResult(_localizer["BusinessMembershipNotFound"]);

            var leave = new MemberLeave
            {
                BusinessMemberId = member.Id,
                Title = requestDto.Title,
                Description = requestDto.Description,
                StartDate = requestDto.StartDate,
                EndDate = requestDto.EndDate,
                DayCount = CalculateLeaveDays(requestDto.StartDate, requestDto.EndDate),
                Status = LeaveStatus.Pending
            };

            _context.MemberLeaves.Add(leave);
            await _context.SaveChangesAsync();
            
            return ServiceResponse<LeaveResponseDto>.SuccessResult(
                MapToResponse(leave, member.User.FirstName + " " + member.User.LastName), 
                _localizer["LeaveRequestCreated"]);
        }

        public async Task<ServiceResponse<List<LeaveResponseDto>>> GetMyLeavesAsync(Guid userId, Guid businessId)
        {
            var member = await _context.BusinessMembers
                .FirstOrDefaultAsync(bm => bm.UserId == userId && bm.BusinessId == businessId && bm.IsActive);
            
            if (member == null) return ServiceResponse<List<LeaveResponseDto>>.ErrorResult(_localizer["MembershipNotFound"]);
            
            var leaves = await _context.MemberLeaves
                .Where(l => l.BusinessMemberId == member.Id)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            
            var list = leaves.Select(l => MapToResponse(l, _localizer["YouLabel"])).ToList();
            return ServiceResponse<List<LeaveResponseDto>>.SuccessResult(list);
        }

        public async Task<ServiceResponse<List<LeaveResponseDto>>> GetBusinessLeavesAsync(Guid userId, Guid businessId)
        {
            var isOwner = await _context.BusinessMembers
                .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == businessId && bm.Role == UserRole.Owner && bm.IsActive);
            
            if (!isOwner) return ServiceResponse<List<LeaveResponseDto>>.ErrorResult(_localizer["UnauthorizedAction"]);
            
            var leaves = await _context.MemberLeaves
                .Include(l => l.BusinessMember)
                .ThenInclude(bm => bm.User)
                .Where(l => l.BusinessMember.BusinessId == businessId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            
            var list = leaves.Select(l => MapToResponse(l, l.BusinessMember.User.FirstName + " " + l.BusinessMember.User.LastName)).ToList();
            return ServiceResponse<List<LeaveResponseDto>>.SuccessResult(list);
        }

        public async Task<ServiceResponse<LeaveResponseDto>> UpdateLeaveStatusAsync(Guid userId, Guid leaveId, UpdateLeaveStatusRequestDto requestDto)
        {
            var leave = await _context.MemberLeaves
                .Include(l => l.BusinessMember)
                .ThenInclude(bm => bm.User)
                .FirstOrDefaultAsync(l => l.Id == leaveId);
            
            if (leave == null) return ServiceResponse<LeaveResponseDto>.ErrorResult(_localizer["LeaveRequestNotFound"]);
            
            var isOwner = await _context.BusinessMembers
                .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == leave.BusinessMember.BusinessId && bm.Role == UserRole.Owner && bm.IsActive);
            
            if (!isOwner) return ServiceResponse<LeaveResponseDto>.ErrorResult(_localizer["UnauthorizedAction"]);
            
            leave.Status = requestDto.Status;
            if (requestDto.Status == LeaveStatus.Rejected)
            {
                leave.RejectionReason = requestDto.RejectionReason;
            }
            leave.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            return ServiceResponse<LeaveResponseDto>.SuccessResult(
                MapToResponse(leave, leave.BusinessMember.User.FirstName + " " + leave.BusinessMember.User.LastName), 
                string.Format(_localizer["LeaveStatusUpdated"], requestDto.Status));
        }

        public async Task<ServiceResponse<bool>> DeleteLeaveAsync(Guid userId, Guid leaveId)
        {
            var leave = await _context.MemberLeaves
                .Include(l => l.BusinessMember)
                .FirstOrDefaultAsync(l => l.Id == leaveId);
            
            if (leave == null) return ServiceResponse<bool>.ErrorResult(_localizer["LeaveRequestNotFound"]);
            
            bool isOwner = await _context.BusinessMembers
                .AnyAsync(bm => bm.UserId == userId && bm.BusinessId == leave.BusinessMember.BusinessId && bm.Role == UserRole.Owner && bm.IsActive);
            
            bool isSelf = leave.BusinessMember.UserId == userId;
            
            if (!isOwner && !isSelf) return ServiceResponse<bool>.ErrorResult(_localizer["UnauthorizedAction"]);
            
            if (isSelf && leave.Status != LeaveStatus.Pending)
            {
                return ServiceResponse<bool>.ErrorResult(_localizer["CannotDeleteProcessedLeave"]);
            }

            _context.MemberLeaves.Remove(leave);
            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResult(true, _localizer["LeaveRequestDeleted"]);
        }
        
        private LeaveResponseDto MapToResponse(MemberLeave leave, string memberName)
        {
            return new LeaveResponseDto
            {
                Id = leave.Id,
                MemberName = memberName,
                Title = leave.Title,
                Description = leave.Description,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                DayCount = leave.DayCount,
                Status = leave.Status.ToString(),
                RejectionReason = leave.RejectionReason,
                CreatedAt = leave.CreatedAt
            };
        }
        
        private static int CalculateLeaveDays(DateTime start, DateTime end)
        {
            var s = start.Date;
            var e = end.Date;
            var days = (e - s).Days + 1; 
            return days < 1 ? 1 : days;
        }
    }
}