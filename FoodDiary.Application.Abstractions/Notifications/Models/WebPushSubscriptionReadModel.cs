namespace FoodDiary.Application.Abstractions.Notifications.Models;

public sealed record WebPushSubscriptionReadModel(
    string Endpoint,
    DateTime? ExpirationTimeUtc,
    string? Locale,
    string? UserAgent,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
