namespace Personelim.Helpers;

public static class SlackEventTypes
{
    public const string TaskCreated = "task_created";

    public static readonly IReadOnlyList<string> All = new[]
    {
        TaskCreated
    };

    public static bool IsValid(string eventType) => All.Contains(eventType);
}
