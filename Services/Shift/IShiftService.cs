using Personelim.DTOs.Shift;
using Personelim.Helpers; // ServiceResponse yapın burada ise

namespace Personelim.Services.Shift
{
    public interface IShiftService
    {
        Task<ServiceResponse<ShiftResponse>> CreateShiftAsync(Guid currentUserId, CreateShiftRequest request);
        Task<ServiceResponse<List<ShiftResponse>>> GetShiftsByBusinessAsync(Guid currentUserId, Guid businessId);
    }
}