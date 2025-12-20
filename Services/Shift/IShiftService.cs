using Personelim.DTOs.Shift;
using Personelim.Helpers;

namespace Personelim.Services.Shift
{
    public interface IShiftService
    {
        Task<ServiceResponse<ShiftResponse>> SubmitShiftAsync(Guid userId, SubmitShiftRequest request);
        Task<ServiceResponse<List<ShiftResponse>>> GetMyShiftsAsync(Guid userId, Guid businessId);
    }
}