using FoodDiary.Modules.Users.Presentation.Contracts.Responses;
using FoodDiary.Modules.Dietologist.Presentation.Contracts.Responses;
using FoodDiary.Modules.Notifications.Presentation.Contracts.Responses;

namespace FoodDiary.Modules.Users.Presentation.Responses;

public sealed record ProfileOverviewHttpResponse(
    UserHttpResponse User,
    NotificationPreferencesHttpResponse NotificationPreferences,
    IReadOnlyList<WebPushSubscriptionHttpResponse> WebPushSubscriptions,
    DietologistRelationshipHttpResponse? DietologistRelationship);
