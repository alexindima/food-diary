namespace FoodDiary.Application.Abstractions.Notifications.Common;

public sealed record ScheduledNotificationData(
    string Type,
    int DelaySeconds,
    DateTime ScheduledAtUtc);
