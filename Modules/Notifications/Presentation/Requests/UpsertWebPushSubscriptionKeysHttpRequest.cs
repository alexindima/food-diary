namespace FoodDiary.Modules.Notifications.Presentation.Requests;

public sealed record UpsertWebPushSubscriptionKeysHttpRequest(
    string P256Dh,
    string Auth);
