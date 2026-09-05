namespace FoodDiary.Presentation.Api.Features.Notifications.Requests;

public sealed record UpsertWebPushSubscriptionKeysHttpRequest(
    string P256Dh,
    string Auth);
