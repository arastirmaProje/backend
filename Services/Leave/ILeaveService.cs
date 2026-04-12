using Personelim.DTOs.Leave;
using Personelim.Helpers;

namespace Personelim.Services.Leave
{
    public interface ILeaveService
    {
        Task<ServiceResponse<LeaveResponseDto>> CreateLeaveRequestAsync(Guid userId, CreateLeaveRequestDto requestDto);
        
        Task<ServiceResponse<List<LeaveResponseDto>>> GetMyLeavesAsync(Guid userId, Guid businessId);
        
        Task<ServiceResponse<List<LeaveResponseDto>>> GetBusinessLeavesAsync(Guid userId, Guid businessId);
        
        Task<ServiceResponse<LeaveResponseDto>> UpdateLeaveStatusAsync(Guid userId, Guid leaveId, UpdateLeaveStatusRequestDto requestDto);
        
        Task<ServiceResponse<bool>> DeleteLeaveAsync(Guid userId, Guid leaveId);
    }
}