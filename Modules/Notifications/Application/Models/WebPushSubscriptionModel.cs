namespace FoodDiary.Modules.Notifications.Application.Models;

public sealed record WebPushSubscriptionModel(
    string Endpoint,
    string EndpointHost,
    DateTime? ExpirationTimeUtc,
    string? Locale,
    string? UserAgent,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
