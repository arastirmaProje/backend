namespace Personelim.Helpers;

public static class SlackEventTypes
{
    public const string TaskCreated    = "task_created";
    public const string MeetingCreated = "meeting_created";
    public const string EventCreated   = "event_created";

    public static readonly IReadOnlyList<string> All = new[]
    {
        TaskCreated,
        MeetingCreated,
        EventCreated
    };

    public static bool IsValid(string eventType) => All.Contains(eventType);
}
