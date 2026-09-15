namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public sealed record ScheduledNotificationData(
    string Type,
    int DelaySeconds,
    DateTime ScheduledAtUtc);
