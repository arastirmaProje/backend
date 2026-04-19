namespace Personelim.DTOs.Slack;

public class UpdateSlackWebhookRequestDto
{
    public string? WebhookUrl { get; set; }
    public string? EventType { get; set; }
    public string? Label { get; set; }
}
