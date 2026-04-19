using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Meeting;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Services.Slack;

namespace Personelim.Services.Meeting;

public class MeetingService : IMeetingService
{
    private readonly AppDbContext _context;
    private readonly ISlackService _slackService;

    public MeetingService(AppDbContext context, ISlackService slackService)
    {
        _context = context;
        _slackService = slackService;
    }

    public async System.Threading.Tasks.Task<ServiceResponse<MeetingResponseDto>> CreateAsync(Guid currentUserId, CreateMeetingRequestDto dto, string type)
    {
        try
        {
            var isMember = await _context.BusinessMembers
                .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == dto.BusinessId && bm.IsActive);
            if (!isMember)
                return ServiceResponse<MeetingResponseDto>.ErrorResult("Bu işlem için yetkiniz yok.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return ServiceResponse<MeetingResponseDto>.ErrorResult("Başlık boş olamaz.");

            var meeting = new Models.Meeting
            {
                BusinessId       = dto.BusinessId,
                CreatedByUserId  = currentUserId,
                Title            = dto.Title.Trim(),
                Description      = dto.Description?.Trim(),
                Date             = dto.Date,
                Type             = type
            };

            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();

            var creator = await _context.Users.FindAsync(currentUserId);
            var result = MapToDto(meeting, creator);

            var slackEvent = type == MeetingTypes.Meeting ? SlackEventTypes.MeetingCreated : SlackEventTypes.EventCreated;
            var typeEmoji  = type == MeetingTypes.Meeting ? "🤝" : "🎉";
            var typeLabel  = type == MeetingTypes.Meeting ? "Toplantı" : "Etkinlik";

            await _slackService.SendAsync(dto.BusinessId, slackEvent, new
            {
                blocks = new object[]
                {
                    new { type = "header", text = new { type = "plain_text", text = $"{typeEmoji} Yeni {typeLabel} Oluşturuldu", emoji = true } },
                    new { type = "divider" },
                    new { type = "section", text = new { type = "mrkdwn", text = $"*{result.Title}*" } },
                    new { type = "section", fields = new[]
                    {
                        new { type = "mrkdwn", text = $"👤 *Oluşturan*\n{result.CreatedByName}" },
                        new { type = "mrkdwn", text = $"📌 *Tür*\n{typeLabel}" },
                        new { type = "mrkdwn", text = $"📅 *Tarih*\n{result.Date:dd MMM yyyy}" },
                        new { type = "mrkdwn", text = $"🕐 *Saat*\n{result.Date:HH:mm}" }
                    }},
                    string.IsNullOrWhiteSpace(result.Description)
                        ? (object)new { type = "divider" }
                        : new { type = "section", text = new { type = "mrkdwn", text = $"📝 *Açıklama*\n{result.Description}" } },
                    new { type = "context", elements = new[] {
                        new { type = "mrkdwn", text = $"🕐 Oluşturulma: {DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC" }
                    }},
                    new { type = "divider" }
                }
            });

            return ServiceResponse<MeetingResponseDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            return ServiceResponse<MeetingResponseDto>.ErrorResult("Oluşturulamadı.", ex.Message);
        }
    }

    public async System.Threading.Tasks.Task<ServiceResponse<List<MeetingResponseDto>>> GetByBusinessAsync(Guid currentUserId, Guid businessId, string type)
    {
        try
        {
            var isMember = await _context.BusinessMembers
                .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == businessId && bm.IsActive);
            if (!isMember)
                return ServiceResponse<List<MeetingResponseDto>>.ErrorResult("Bu işlem için yetkiniz yok.");

            var meetings = await _context.Meetings
                .Include(m => m.CreatedBy)
                .Where(m => m.BusinessId == businessId && m.Type == type && m.IsActive)
                .OrderBy(m => m.Date)
                .ToListAsync();

            return ServiceResponse<List<MeetingResponseDto>>.SuccessResult(
                meetings.Select(m => MapToDto(m, m.CreatedBy)).ToList());
        }
        catch (Exception ex)
        {
            return ServiceResponse<List<MeetingResponseDto>>.ErrorResult("Listelenemedi.", ex.Message);
        }
    }

    public async System.Threading.Tasks.Task<ServiceResponse<bool>> DeleteAsync(Guid currentUserId, Guid meetingId)
    {
        try
        {
            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting == null)
                return ServiceResponse<bool>.ErrorResult("Bulunamadı.");

            var isOwnerOrCreator = meeting.CreatedByUserId == currentUserId ||
                await _context.Businesses.AnyAsync(b => b.Id == meeting.BusinessId && b.OwnerId == currentUserId);
            if (!isOwnerOrCreator)
                return ServiceResponse<bool>.ErrorResult("Bu işlem için yetkiniz yok.");

            meeting.IsActive = false;
            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.ErrorResult("Silinemedi.", ex.Message);
        }
    }

    private static MeetingResponseDto MapToDto(Models.Meeting m, User? creator) => new()
    {
        Id            = m.Id,
        BusinessId    = m.BusinessId,
        Title         = m.Title,
        Description   = m.Description,
        Date          = m.Date,
        Type          = m.Type,
        CreatedByName = creator != null ? $"{creator.FirstName} {creator.LastName}" : string.Empty,
        CreatedAt     = m.CreatedAt
    };
}
