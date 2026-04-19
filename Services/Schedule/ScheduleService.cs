using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Schedule;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using Personelim.Services.Slack;

namespace Personelim.Services.Schedule;

public class ScheduleService : IScheduleService
{
    private readonly AppDbContext _context;
    private readonly ISlackService _slackService;

    public ScheduleService(AppDbContext context, ISlackService slackService)
    {
        _context = context;
        _slackService = slackService;
    }

    public async System.Threading.Tasks.Task<ServiceResponse<ScheduleResponseDto>> CreateAsync(Guid currentUserId, CreateScheduleRequestDto dto)
    {
        try
        {
            var isMember = await _context.BusinessMembers
                .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == dto.BusinessId && bm.IsActive);
            if (!isMember)
                return ServiceResponse<ScheduleResponseDto>.ErrorResult("Bu işlem için yetkiniz yok.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return ServiceResponse<ScheduleResponseDto>.ErrorResult("Başlık boş olamaz.");

            var typeStr = ToDbString(dto.Type);

            var schedule = new Models.Schedule
            {
                BusinessId      = dto.BusinessId,
                CreatedByUserId = currentUserId,
                Title           = dto.Title.Trim(),
                Description     = dto.Description?.Trim(),
                Date            = dto.Date,
                Type            = typeStr
            };

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            var creator    = await _context.Users.FindAsync(currentUserId);
            var result     = MapToDto(schedule, creator);
            var typeLabel  = dto.Type == ScheduleType.Meeting ? "Toplantı" : "Etkinlik";
            var typeEmoji  = dto.Type == ScheduleType.Meeting ? "🤝" : "🎉";
            var slackEvent = dto.Type == ScheduleType.Meeting ? SlackEventTypes.MeetingCreated : SlackEventTypes.EventCreated;

            var color = dto.Type == ScheduleType.Meeting ? "#8B5CF6" : "#F59E0B";

            await _slackService.SendAsync(dto.BusinessId, slackEvent, new
            {
                attachments = new object[]
                {
                    new
                    {
                        color,
                        blocks = new object[]
                        {
                            new { type = "section", text = new { type = "mrkdwn", text = $"{typeEmoji} *{typeLabel} Duyurusu*" } },
                            new { type = "section", text = new { type = "mrkdwn", text = $"*{result.Title}*" } },
                            new { type = "section", fields = new[]
                            {
                                new { type = "mrkdwn", text = $"*Tarih*\n{result.Date:dd MMM yyyy}" },
                                new { type = "mrkdwn", text = $"*Saat*\n{result.Date:HH:mm}" },
                                new { type = "mrkdwn", text = $"*Düzenleyen*\n{result.CreatedByName}" }
                            }},
                            string.IsNullOrWhiteSpace(result.Description)
                                ? (object)new { type = "divider" }
                                : new { type = "section", text = new { type = "mrkdwn", text = $"_{result.Description}_" } },
                            new { type = "context", elements = new object[] {
                                new { type = "mrkdwn", text = $"Takvime ekle: *{result.Date:dd MMM yyyy, HH:mm}*" }
                            }}
                        }
                    }
                }
            });

            return ServiceResponse<ScheduleResponseDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            return ServiceResponse<ScheduleResponseDto>.ErrorResult("Oluşturulamadı.", ex.Message);
        }
    }

    public async System.Threading.Tasks.Task<ServiceResponse<List<ScheduleResponseDto>>> GetByBusinessAsync(Guid currentUserId, Guid businessId, ScheduleType? type)
    {
        try
        {
            var isMember = await _context.BusinessMembers
                .AnyAsync(bm => bm.UserId == currentUserId && bm.BusinessId == businessId && bm.IsActive);
            if (!isMember)
                return ServiceResponse<List<ScheduleResponseDto>>.ErrorResult("Bu işlem için yetkiniz yok.");

            var query = _context.Schedules
                .Include(s => s.CreatedBy)
                .Where(s => s.BusinessId == businessId && s.IsActive);

            if (type.HasValue)
            {
                var typeStr = ToDbString(type.Value);
                query = query.Where(s => s.Type == typeStr);
            }

            var schedules = await query.OrderBy(s => s.Date).ToListAsync();

            return ServiceResponse<List<ScheduleResponseDto>>.SuccessResult(
                schedules.Select(s => MapToDto(s, s.CreatedBy)).ToList());
        }
        catch (Exception ex)
        {
            return ServiceResponse<List<ScheduleResponseDto>>.ErrorResult("Listelenemedi.", ex.Message);
        }
    }

    public async System.Threading.Tasks.Task<ServiceResponse<bool>> DeleteAsync(Guid currentUserId, Guid scheduleId)
    {
        try
        {
            var schedule = await _context.Schedules.FindAsync(scheduleId);
            if (schedule == null)
                return ServiceResponse<bool>.ErrorResult("Bulunamadı.");

            var isOwnerOrCreator = schedule.CreatedByUserId == currentUserId ||
                await _context.Businesses.AnyAsync(b => b.Id == schedule.BusinessId && b.OwnerId == currentUserId);
            if (!isOwnerOrCreator)
                return ServiceResponse<bool>.ErrorResult("Bu işlem için yetkiniz yok.");

            schedule.IsActive = false;
            await _context.SaveChangesAsync();
            return ServiceResponse<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.ErrorResult("Silinemedi.", ex.Message);
        }
    }

    private static string ToDbString(ScheduleType type) => type switch
    {
        ScheduleType.Meeting => "toplantı",
        ScheduleType.Event   => "etkinlik",
        _                    => "toplantı"
    };

    private static ScheduleType FromDbString(string type) => type switch
    {
        "etkinlik" => ScheduleType.Event,
        _          => ScheduleType.Meeting
    };

    private static ScheduleResponseDto MapToDto(Models.Schedule s, User? creator) => new()
    {
        Id            = s.Id,
        BusinessId    = s.BusinessId,
        Title         = s.Title,
        Description   = s.Description,
        Date          = s.Date,
        Type          = FromDbString(s.Type),
        CreatedByName = creator != null ? $"{creator.FirstName} {creator.LastName}" : string.Empty,
        CreatedAt     = s.CreatedAt
    };
}
