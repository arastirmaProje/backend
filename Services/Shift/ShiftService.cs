using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Personelim.Data;
using Personelim.DTOs.Shift;
using Personelim.Helpers;
using Personelim.Resources;

namespace Personelim.Services.Shift
{
    public class ShiftService : IShiftService
    {
        private readonly AppDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ShiftService(AppDbContext context, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<ServiceResponse<ShiftResponseDto>> SubmitShiftAsync(Guid userId, SubmitShiftRequestDto requestDto)
        {
            try
            {
                if (requestDto.BusinessId == Guid.Empty)
                    return ServiceResponse<ShiftResponseDto>.ErrorResult(_localizer["BusinessIdRequired"]);
                
                if (requestDto.EndTime <= requestDto.StartTime)
                    return ServiceResponse<ShiftResponseDto>.ErrorResult(_localizer["EndTimeBeforeStartTime"]);

                var totalHours = (decimal)(requestDto.EndTime - requestDto.StartTime).TotalHours;
                
                var shift = new Models.Shift
                {
                    BusinessId = requestDto.BusinessId,
                    UserId = userId,
                    StartTime = requestDto.StartTime,
                    EndTime = requestDto.EndTime,
                    TotalHours = Math.Round(totalHours, 2),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Shifts.Add(shift);
                await _context.SaveChangesAsync();

                return ServiceResponse<ShiftResponseDto>.SuccessResult(new ShiftResponseDto
                {
                    Id = shift.Id,
                    BusinessId = shift.BusinessId,
                    UserId = shift.UserId,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    TotalHours = (double)shift.TotalHours,
                    CreatedAt = shift.CreatedAt
                }, _localizer["ShiftSaved"]);
            }
            catch (Exception ex)
            {
                return ServiceResponse<ShiftResponseDto>.ErrorResult(_localizer["ShiftSaveError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<List<ShiftResponseDto>>> GetMyShiftsAsync(Guid userId, Guid businessId)
        {
            try
            {
                var shifts = await _context.Shifts
                    .Where(s => s.BusinessId == businessId && s.UserId == userId)
                    .OrderByDescending(s => s.StartTime)
                    .ToListAsync();

                var list = shifts.Select(s => new ShiftResponseDto
                {
                    Id = s.Id,
                    BusinessId = s.BusinessId,
                    UserId = s.UserId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    TotalHours = (double)s.TotalHours,
                    CreatedAt = s.CreatedAt
                }).ToList();

                return ServiceResponse<List<ShiftResponseDto>>.SuccessResult(list);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<ShiftResponseDto>>.ErrorResult(_localizer["ShiftsFetchError"], ex.Message);
            }
        }
    }
}