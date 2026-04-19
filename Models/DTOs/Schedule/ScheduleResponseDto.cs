using Personelim.Models.Enums;

namespace Personelim.DTOs.Schedule;

public class ScheduleResponseDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public ScheduleType Type { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
