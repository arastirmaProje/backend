using Personelim.DTOs.Schedule;
using Personelim.Helpers;
using Personelim.Models.Enums;

namespace Personelim.Services.Schedule;

public interface IScheduleService
{
    System.Threading.Tasks.Task<ServiceResponse<ScheduleResponseDto>> CreateAsync(Guid currentUserId, CreateScheduleRequestDto dto);
    System.Threading.Tasks.Task<ServiceResponse<List<ScheduleResponseDto>>> GetByBusinessAsync(Guid currentUserId, Guid businessId, ScheduleType? type);
    System.Threading.Tasks.Task<ServiceResponse<bool>> DeleteAsync(Guid currentUserId, Guid scheduleId);
}
