using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserProfileState(
    string? Username,
    string? FirstName,
    string? LastName,
    DateTime? BirthDate,
    string? Gender,
    double? Weight,
    double? Height,
    ActivityLevel ActivityLevel,
    string? ProfileImage,
    ImageAssetId? ProfileImageAssetId,
    string? DashboardLayoutJson,
    string? Language,
    bool PushNotificationsEnabled,
    bool FastingPushNotificationsEnabled,
    bool SocialPushNotificationsEnabled) {
    public static UserProfileState CreateInitial() {
        return new UserProfileState(
            Username: null,
            FirstName: null,
            LastName: null,
            BirthDate: null,
            Gender: null,
            Weight: null,
            Height: null,
            ActivityLevel: ActivityLevel.Moderate,
            ProfileImage: null,
            ProfileImageAssetId: null,
            DashboardLayoutJson: null,
            Language: null,
            PushNotificationsEnabled: false,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true);
    }

    public UserPersonalProfileState PersonalInfo => new(
        Username,
        FirstName,
        LastName,
        BirthDate,
        Gender,
        Weight,
        Height,
        ActivityLevel);

    public UserProfileMediaState Media => new(ProfileImage, ProfileImageAssetId);

    public UserPreferenceState Preferences => new(
        DashboardLayoutJson,
        Language,
        PushNotificationsEnabled,
        FastingPushNotificationsEnabled,
        SocialPushNotificationsEnabled);
}
