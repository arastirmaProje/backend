using Personelim.DTOs.Shift;
using Personelim.Helpers;

namespace Personelim.Services.Shift
{
    public interface IShiftService
    {
        Task<ServiceResponse<ShiftResponseDto>> SubmitShiftAsync(Guid userId, SubmitShiftRequestDto requestDto);
        Task<ServiceResponse<List<ShiftResponseDto>>> GetMyShiftsAsync(Guid userId, Guid businessId);
    }
}