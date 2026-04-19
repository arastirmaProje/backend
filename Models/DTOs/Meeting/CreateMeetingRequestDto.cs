namespace Personelim.DTOs.Meeting;

public class CreateMeetingRequestDto
{
    public Guid BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
}
