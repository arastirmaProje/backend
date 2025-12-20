using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Shift;
using Personelim.Helpers;

namespace Personelim.Services.Shift
{
    public class ShiftService : IShiftService
    {
        private readonly AppDbContext _context;

        public ShiftService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResponse<ShiftResponse>> SubmitShiftAsync(Guid userId, SubmitShiftRequest request)
        {
            try
            {
                if (request.BusinessId == Guid.Empty)
                    return ServiceResponse<ShiftResponse>.ErrorResult("BusinessId zorunludur.");

                if (request.EndTime <= request.StartTime)
                    return ServiceResponse<ShiftResponse>.ErrorResult("Bitiş zamanı başlangıçtan sonra olmalı.");

                var totalHours = (decimal)(request.EndTime - request.StartTime).TotalHours;

                var shift = new Models.Shift
                {
                    BusinessId = request.BusinessId,
                    UserId = userId,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    TotalHours = Math.Round(totalHours, 2),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Shifts.Add(shift);
                await _context.SaveChangesAsync();

                return ServiceResponse<ShiftResponse>.SuccessResult(new ShiftResponse
                {
                    Id = shift.Id,
                    BusinessId = shift.BusinessId,
                    UserId = shift.UserId,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    TotalHours = (double)shift.TotalHours,
                    CreatedAt = shift.CreatedAt
                }, "Mesai kaydedildi.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<ShiftResponse>.ErrorResult("Mesai kaydedilemedi", ex.Message);
            }
        }

        public async Task<ServiceResponse<List<ShiftResponse>>> GetMyShiftsAsync(Guid userId, Guid businessId)
        {
            try
            {
                var shifts = await _context.Shifts
                    .Where(s => s.BusinessId == businessId && s.UserId == userId)
                    .OrderByDescending(s => s.StartTime)
                    .ToListAsync();

                var list = shifts.Select(s => new ShiftResponse
                {
                    Id = s.Id,
                    BusinessId = s.BusinessId,
                    UserId = s.UserId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    TotalHours = (double)s.TotalHours,
                    CreatedAt = s.CreatedAt
                }).ToList();

                return ServiceResponse<List<ShiftResponse>>.SuccessResult(list);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<ShiftResponse>>.ErrorResult("Mesailer getirilemedi", ex.Message);
            }
        }
    }
}