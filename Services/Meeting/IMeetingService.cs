using Personelim.DTOs.Meeting;
using Personelim.Helpers;
using Personelim.Models;

namespace Personelim.Services.Meeting;

public interface IMeetingService
{
    System.Threading.Tasks.Task<ServiceResponse<MeetingResponseDto>> CreateAsync(Guid currentUserId, CreateMeetingRequestDto dto, string type);
    System.Threading.Tasks.Task<ServiceResponse<List<MeetingResponseDto>>> GetByBusinessAsync(Guid currentUserId, Guid businessId, string type);
    System.Threading.Tasks.Task<ServiceResponse<bool>> DeleteAsync(Guid currentUserId, Guid meetingId);
}
