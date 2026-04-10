namespace FoodDiary.Presentation.Api.Features.Notifications.Requests;

public sealed record UpsertWebPushSubscriptionHttpRequest(
    string Endpoint,
    DateTime? ExpirationTime,
    UpsertWebPushSubscriptionKeysHttpRequest Keys,
    string? Locale = null,
    string? UserAgent = null);

public sealed record UpsertWebPushSubscriptionKeysHttpRequest(
    string P256dh,
    string Auth);
