namespace FoodDiary.Modules.Notifications.Application.Models;

public sealed record ScheduledNotificationModel(
    string Type,
    int DelaySeconds,
    DateTime ScheduledAtUtc);
