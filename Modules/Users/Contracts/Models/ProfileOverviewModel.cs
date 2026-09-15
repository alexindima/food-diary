namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record ProfileOverviewModel(
    UserModel User,
    UserNotificationPreferencesModel NotificationPreferences,
    IReadOnlyList<ProfileWebPushSubscriptionModel> WebPushSubscriptions,
    ProfileDietologistRelationshipModel? DietologistRelationship);
