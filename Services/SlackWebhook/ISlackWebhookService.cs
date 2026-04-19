using Personelim.DTOs.Slack;
using Personelim.Helpers;
using Personelim.Models;

namespace Personelim.Services.SlackWebhook;

public interface ISlackWebhookService
{
    System.Threading.Tasks.Task<ServiceResponse<SlackWebhookResponseDto>> CreateAsync(Guid currentUserId, CreateSlackWebhookRequestDto dto);
    System.Threading.Tasks.Task<ServiceResponse<List<SlackWebhookResponseDto>>> GetByBusinessAsync(Guid currentUserId, Guid businessId);
    System.Threading.Tasks.Task<ServiceResponse<bool>> DeleteAsync(Guid currentUserId, Guid webhookId);
    System.Threading.Tasks.Task<ServiceResponse<bool>> ToggleActiveAsync(Guid currentUserId, Guid webhookId);
    System.Threading.Tasks.Task<ServiceResponse<SlackWebhookResponseDto>> UpdateAsync(Guid currentUserId, Guid webhookId, UpdateSlackWebhookRequestDto dto);
}
