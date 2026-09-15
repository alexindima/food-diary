namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record ProfileWebPushSubscriptionModel(
    string Endpoint,
    string EndpointHost,
    DateTime? ExpirationTimeUtc,
    string? Locale,
    string? UserAgent,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
