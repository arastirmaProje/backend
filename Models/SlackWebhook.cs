namespace Personelim.Models;

public class SlackWebhook
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Business? Business { get; set; }

    public SlackWebhook()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}
