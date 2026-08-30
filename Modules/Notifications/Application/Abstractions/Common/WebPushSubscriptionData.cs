namespace FoodDiary.Application.Abstractions.Notifications.Common;

public sealed record WebPushSubscriptionData(
    string Endpoint,
    string P256Dh,
    string Auth,
    DateTime? ExpirationTimeUtc,
    string? Locale,
    string? UserAgent);
