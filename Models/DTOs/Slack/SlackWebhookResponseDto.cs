namespace Personelim.DTOs.Slack;

public class SlackWebhookResponseDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
