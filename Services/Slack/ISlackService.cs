namespace Personelim.Services.Slack;

public interface ISlackService
{
    System.Threading.Tasks.Task SendAsync(Guid businessId, string eventType, object payload);
}
