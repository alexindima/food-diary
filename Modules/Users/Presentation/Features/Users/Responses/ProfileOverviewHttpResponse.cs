using FoodDiary.Modules.Dietologist.Presentation.Contracts.Responses;
using FoodDiary.Presentation.Api.Features.Notifications.Responses;

namespace FoodDiary.Presentation.Api.Features.Users.Responses;

public sealed record ProfileOverviewHttpResponse(
    UserHttpResponse User,
    NotificationPreferencesHttpResponse NotificationPreferences,
    IReadOnlyList<WebPushSubscriptionHttpResponse> WebPushSubscriptions,
    DietologistRelationshipHttpResponse? DietologistRelationship);
