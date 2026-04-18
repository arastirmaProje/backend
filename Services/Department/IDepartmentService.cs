using Personelim.DTOs.Department;
using Personelim.Helpers;

namespace Personelim.Services.Department
{
    public interface IDepartmentService
    {
        Task<ServiceResponse<List<DepartmentResponseDto>>> GetDepartmentsByBusinessIdAsync(Guid currentUserId, Guid businessId);
        Task<ServiceResponse<DepartmentResponseDto>> CreateDepartmentAsync(Guid currentUserId, CreateDepartmentRequestDto requestDto);
        Task<ServiceResponse<DepartmentResponseDto>> UpdateDepartmentAsync(Guid currentUserId, Guid departmentId, UpdateDepartmentRequestDto requestDto);
        Task<ServiceResponse<bool>> DeleteDepartmentAsync(Guid currentUserId, Guid departmentId);
    }
}
