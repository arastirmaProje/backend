namespace Personelim.DTOs.Meeting;

public class MeetingResponseDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
