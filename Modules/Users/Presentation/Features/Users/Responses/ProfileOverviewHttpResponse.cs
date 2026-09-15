using FoodDiary.Modules.Dietologist.Presentation.Contracts.Responses;
using FoodDiary.Modules.Notifications.Presentation.Contracts.Responses;

namespace FoodDiary.Presentation.Api.Features.Users.Responses;

public sealed record ProfileOverviewHttpResponse(
    UserHttpResponse User,
    NotificationPreferencesHttpResponse NotificationPreferences,
    IReadOnlyList<WebPushSubscriptionHttpResponse> WebPushSubscriptions,
    DietologistRelationshipHttpResponse? DietologistRelationship);
