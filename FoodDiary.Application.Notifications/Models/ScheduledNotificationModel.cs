namespace FoodDiary.Application.Notifications.Models;

public sealed record ScheduledNotificationModel(
    string Type,
    int DelaySeconds,
    DateTime ScheduledAtUtc);
