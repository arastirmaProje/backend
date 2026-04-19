namespace Personelim.DTOs.Slack;

public class CreateSlackWebhookRequestDto
{
    public Guid BusinessId { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
