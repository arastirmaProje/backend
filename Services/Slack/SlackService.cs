using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Personelim.Data;

namespace Personelim.Services.Slack;

public class SlackService : ISlackService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;
    private readonly ILogger<SlackService> _logger;

    public SlackService(IHttpClientFactory httpClientFactory, AppDbContext context, ILogger<SlackService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task SendAsync(Guid businessId, string eventType, object payload)
    {
        var webhooks = await _context.SlackWebhooks
            .Where(w => w.BusinessId == businessId && w.EventType == eventType && w.IsActive)
            .ToListAsync();

        if (!webhooks.Any())
            return;

        var client = _httpClientFactory.CreateClient();
        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        foreach (var webhook in webhooks)
        {
            try
            {
                var response = await client.PostAsync(webhook.WebhookUrl, content);
                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("Slack webhook başarısız. Label: {Label}, Status: {Status}", webhook.Label, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Slack webhook gönderilemedi. Label: {Label}", webhook.Label);
            }
        }
    }
}
