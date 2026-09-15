namespace FoodDiary.Modules.Notifications.Presentation.Responses;

public sealed record ScheduledNotificationHttpResponse(
    string Type,
    int DelaySeconds,
    DateTime ScheduledAtUtc);
