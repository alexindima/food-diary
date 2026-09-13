namespace FoodDiary.Presentation.Api.Features.Notifications.Responses;

public sealed record WebPushSubscriptionHttpResponse(
    string Endpoint,
    string EndpointHost,
    DateTime? ExpirationTimeUtc,
    string? Locale,
    string? UserAgent,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
