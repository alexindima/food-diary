using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Users.Models;

public sealed record ProfileOverviewModel(
    UserModel User,
    NotificationPreferencesModel NotificationPreferences,
    IReadOnlyList<WebPushSubscriptionModel> WebPushSubscriptions,
    DietologistRelationshipModel? DietologistRelationship);
