using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Slack;
using Personelim.Helpers;
using Personelim.Models.Enums;

namespace Personelim.Services.SlackWebhook;

public class SlackWebhookService : ISlackWebhookService
{
    private readonly AppDbContext _context;

    public SlackWebhookService(AppDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<ServiceResponse<SlackWebhookResponseDto>> CreateAsync(Guid currentUserId, CreateSlackWebhookRequestDto dto)
    {
        try
        {
            if (!await HasManagementAccess(currentUserId, dto.BusinessId))
                return ServiceResponse<SlackWebhookResponseDto>.ErrorResult("Bu işlem için yetkiniz yok.");

            if (string.IsNullOrWhiteSpace(dto.WebhookUrl) || !dto.WebhookUrl.StartsWith("https://hooks.slack.com/"))
                return ServiceResponse<SlackWebhookResponseDto>.ErrorResult("Geçersiz Slack webhook URL.");

            if (!SlackEventTypes.IsValid(dto.EventType))
                return ServiceResponse<SlackWebhookResponseDto>.ErrorResult($"Geçersiz eventType. Geçerli değerler: {string.Join(", ", SlackEventTypes.All)}");

            var webhook = new Models.SlackWebhook
            {
                BusinessId = dto.BusinessId,
                WebhookUrl = dto.WebhookUrl.Trim(),
                EventType  = dto.EventType.Trim().ToLowerInvariant(),
                Label      = dto.Label.Trim()
            };

            _context.SlackWebhooks.Add(webhook);
            await _context.SaveChangesAsync();

            return ServiceResponse<SlackWebhookResponseDto>.SuccessResult(MapToDto(webhook));
        }
        catch (Exception ex)
        {
            return ServiceResponse<SlackWebhookResponseDto>.ErrorResult("Webhook eklenemedi.", ex.Message);
        }
    }

    public async System.Threading.Tasks.Task<ServiceResponse<List<SlackWebhookResponseDto>>> GetByBusinessAsync(Guid currentUserId, Guid businessId)
    {
        try
        {
            if (!await HasManagementAccess(currentUserId, businessId))
                return ServiceResponse<List<SlackWebhookResponseDto>>.ErrorResult("Bu işlem için yetkiniz yok.");

            var webhooks = await _context.SlackWebhooks
                .Where(w => w.BusinessId == businessId)
                .OrderBy(w => w.EventType)
                .ToListAsync();

            return ServiceResponse<List<SlackWebhookResponseDto>>.SuccessResult(webhooks.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            return ServiceResponse<List<SlackWebhookResponseDto>>.ErrorResult("Webhooklar getirilemedi.", ex.Message);
        }
    }

    public async System.Threading.Tasks.Task<ServiceResponse<bool>> DeleteAsync(Guid currentUserId, Guid webhookId)
    {
        try
        {
            var webhook = await _context.SlackWebhooks.FindAsync(webhookId);
            if (webhook == null)
                return ServiceResponse<bool>.ErrorResult("Webhook bulunamadı.");

            if (!await HasManagementAccess(currentUserId, webhook.BusinessId))
                return ServiceResponse<bool>.ErrorResult("Bu işlem için yetkiniz yok.");

            _context.SlackWebhooks.Remove(webhook);
            await _context.SaveChangesAsync();

            return ServiceResponse<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.ErrorResult("Webhook silinemedi.", ex.Message);
        }
    }

    public async System.Threading.Tasks.Task<ServiceResponse<bool>> ToggleActiveAsync(Guid currentUserId, Guid webhookId)
    {
        try
        {
            var webhook = await _context.SlackWebhooks.FindAsync(webhookId);
            if (webhook == null)
                return ServiceResponse<bool>.ErrorResult("Webhook bulunamadı.");

            if (!await HasManagementAccess(currentUserId, webhook.BusinessId))
                return ServiceResponse<bool>.ErrorResult("Bu işlem için yetkiniz yok.");

            webhook.IsActive = !webhook.IsActive;
            await _context.SaveChangesAsync();

            return ServiceResponse<bool>.SuccessResult(webhook.IsActive);
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.ErrorResult("Webhook güncellenemedi.", ex.Message);
        }
    }

    public async System.Threading.Tasks.Task<ServiceResponse<SlackWebhookResponseDto>> UpdateAsync(Guid currentUserId, Guid webhookId, UpdateSlackWebhookRequestDto dto)
    {
        try
        {
            var webhook = await _context.SlackWebhooks.FindAsync(webhookId);
            if (webhook == null)
                return ServiceResponse<SlackWebhookResponseDto>.ErrorResult("Webhook bulunamadı.");

            if (!await HasManagementAccess(currentUserId, webhook.BusinessId))
                return ServiceResponse<SlackWebhookResponseDto>.ErrorResult("Bu işlem için yetkiniz yok.");

            if (dto.WebhookUrl != null)
            {
                if (!dto.WebhookUrl.StartsWith("https://hooks.slack.com/"))
                    return ServiceResponse<SlackWebhookResponseDto>.ErrorResult("Geçersiz Slack webhook URL.");
                webhook.WebhookUrl = dto.WebhookUrl.Trim();
            }

            if (dto.EventType != null)
                webhook.EventType = dto.EventType.Trim().ToLowerInvariant();

            if (dto.Label != null)
                webhook.Label = dto.Label.Trim();

            await _context.SaveChangesAsync();
            return ServiceResponse<SlackWebhookResponseDto>.SuccessResult(MapToDto(webhook));
        }
        catch (Exception ex)
        {
            return ServiceResponse<SlackWebhookResponseDto>.ErrorResult("Webhook güncellenemedi.", ex.Message);
        }
    }

    private async System.Threading.Tasks.Task<bool> HasManagementAccess(Guid userId, Guid businessId)
    {
        var isOwner = await _context.Businesses
            .AnyAsync(b => b.Id == businessId && b.OwnerId == userId);
        if (isOwner) return true;

        var member = await _context.BusinessMembers
            .FirstOrDefaultAsync(bm => bm.UserId == userId && bm.BusinessId == businessId && bm.IsActive);
        if (member == null) return false;
        return JobTitles.GetRole(member.Position) >= UserRole.Manager;
    }

    private static SlackWebhookResponseDto MapToDto(Models.SlackWebhook w) => new()
    {
        Id         = w.Id,
        BusinessId = w.BusinessId,
        WebhookUrl = w.WebhookUrl,
        EventType  = w.EventType,
        Label      = w.Label,
        IsActive   = w.IsActive,
        CreatedAt  = w.CreatedAt
    };
}
