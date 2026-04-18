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
    string? Theme,
    string? UiStyle,
    bool PushNotificationsEnabled,
    bool FastingPushNotificationsEnabled,
    bool SocialPushNotificationsEnabled,
    int FastingCheckInReminderHours,
    int FastingCheckInFollowUpReminderHours) {
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
            Theme: ThemeCode.Default.Value,
            UiStyle: UiStyleCode.Default.Value,
            PushNotificationsEnabled: false,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true,
            FastingCheckInReminderHours: 12,
            FastingCheckInFollowUpReminderHours: 20);
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
        Theme,
        UiStyle,
        PushNotificationsEnabled,
        FastingPushNotificationsEnabled,
        SocialPushNotificationsEnabled,
        FastingCheckInReminderHours,
        FastingCheckInFollowUpReminderHours);
}
