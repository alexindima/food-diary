namespace FoodDiary.Application.Users.Models;

public sealed record ProfileOverviewModel(
    UserModel User,
    UserNotificationPreferencesModel NotificationPreferences,
    IReadOnlyList<ProfileWebPushSubscriptionModel> WebPushSubscriptions,
    ProfileDietologistRelationshipModel? DietologistRelationship);
