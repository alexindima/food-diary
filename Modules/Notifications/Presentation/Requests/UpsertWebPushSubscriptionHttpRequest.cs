namespace FoodDiary.Modules.Notifications.Presentation.Requests;

public sealed record UpsertWebPushSubscriptionHttpRequest(
    string Endpoint,
    DateTime? ExpirationTime,
    UpsertWebPushSubscriptionKeysHttpRequest Keys,
    string? Locale = null,
    string? UserAgent = null);
