namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record ProfileOverviewModel(
    UserModel User,
    UserNotificationPreferencesModel NotificationPreferences,
    IReadOnlyList<ProfileWebPushSubscriptionModel> WebPushSubscriptions,
    ProfileDietologistRelationshipModel? DietologistRelationship);
