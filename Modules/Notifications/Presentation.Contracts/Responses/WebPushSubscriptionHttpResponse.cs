namespace FoodDiary.Modules.Notifications.Presentation.Contracts.Responses;

public sealed record WebPushSubscriptionHttpResponse(
    string Endpoint,
    string EndpointHost,
    DateTime? ExpirationTimeUtc,
    string? Locale,
    string? UserAgent,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
