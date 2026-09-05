namespace FoodDiary.Presentation.Api.Features.Notifications.Responses;

public sealed record ScheduledNotificationHttpResponse(
    string Type,
    int DelaySeconds,
    DateTime ScheduledAtUtc);
