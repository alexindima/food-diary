using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void AcceptAiConsent() {
        EnsureNotDeleted();
        if (AiConsentAcceptedAt is not null) {
            return;
        }

        AiConsentAcceptedAt = DomainTime.UtcNow;
        SetModified();
    }

    public void RevokeAiConsent() {
        EnsureNotDeleted();
        if (AiConsentAcceptedAt is null) {
            return;
        }

        AiConsentAcceptedAt = null;
        SetModified();
    }

    public void SetLanguage(string language) {
        EnsureNotDeleted();
        if (ApplyPreferencesChanges(
            dashboardLayoutJson: null,
            language: language,
            theme: null,
            uiStyle: null,
            pushNotificationsEnabled: null,
            fastingPushNotificationsEnabled: null,
            socialPushNotificationsEnabled: null,
            fastingCheckInReminderHours: null,
            fastingCheckInFollowUpReminderHours: null)) {
            SetModified();
        }
    }

    public void LinkTelegram(long telegramUserId) {
        EnsureNotDeleted();
        ApplyAccountState(GetAccountState().WithTelegram(telegramUserId));
        SetModified();
    }

    public void UnlinkTelegram() {
        EnsureNotDeleted();
        ApplyAccountState(GetAccountState().WithTelegram(null));
        SetModified();
    }

    public void UpdatePersonalInfo(
        string? username = null,
        string? firstName = null,
        string? lastName = null,
        DateTime? birthDate = null,
        string? gender = null,
        double? weight = null,
        double? height = null) {
        UpdatePersonalInfo(new UserPersonalInfoUpdate(
            Username: username,
            FirstName: firstName,
            LastName: lastName,
            BirthDate: birthDate,
            Gender: gender,
            Weight: weight,
            Height: height));
    }

    public void UpdatePersonalInfo(UserPersonalInfoUpdate update) {
        EnsureNotDeleted();
        if (ApplyPersonalInfoChanges(
            update.Username,
            update.FirstName,
            update.LastName,
            update.BirthDate,
            update.Gender,
            update.Weight,
            update.Height)) {
            SetModified();
        }
    }

    public void UpdateActivity(
        ActivityLevel? activityLevel = null,
        int? stepGoal = null,
        double? hydrationGoal = null) {
        UpdateActivity(new UserActivityUpdate(activityLevel, stepGoal, hydrationGoal));
    }

    public void UpdateActivity(UserActivityUpdate update) {
        EnsureNotDeleted();
        if (ApplyActivityChanges(update.ActivityLevel, update.StepGoal, update.HydrationGoal)) {
            SetModified();
        }
    }

    public void UpdatePreferences(UserPreferenceUpdate update) {
        EnsureNotDeleted();
        if (ApplyPreferencesChanges(
            update.DashboardLayoutJson,
            update.Language,
            update.Theme,
            update.UiStyle,
            update.PushNotificationsEnabled,
            update.FastingPushNotificationsEnabled,
            update.SocialPushNotificationsEnabled,
            update.FastingCheckInReminderHours,
            update.FastingCheckInFollowUpReminderHours)) {
            SetModified();
        }
    }

    public void UpdateProfileMedia(
        string? profileImage = null,
        ImageAssetId? profileImageAssetId = null) {
        UpdateProfileMedia(new UserProfileMediaUpdate(profileImage, profileImageAssetId));
    }

    public void UpdateProfileMedia(UserProfileMediaUpdate update) {
        EnsureNotDeleted();
        if (ApplyProfileMediaChanges(update.ProfileImage, update.ProfileImageAssetId)) {
            SetModified();
        }
    }

    private bool ApplyPersonalInfoChanges(
        string? username,
        string? firstName,
        string? lastName,
        DateTime? birthDate,
        string? gender,
        double? weight,
        double? height) {
        var normalizedUsername = NormalizeOptionalProfileText(username);
        var normalizedFirstName = NormalizeOptionalProfileText(firstName);
        var normalizedLastName = NormalizeOptionalProfileText(lastName);

        EnsureBirthDateIsNotFuture(birthDate);
        EnsurePositive(weight, nameof(weight));
        EnsurePositive(height, nameof(height));
        EnsureGender(gender, nameof(gender));

        var state = GetPersonalProfileState();
        var changed = false;

        if (username is not null && state.Username != normalizedUsername) {
            state = state with { Username = normalizedUsername };
            changed = true;
        }

        if (firstName is not null && state.FirstName != normalizedFirstName) {
            state = state with { FirstName = normalizedFirstName };
            changed = true;
        }

        if (lastName is not null && state.LastName != normalizedLastName) {
            state = state with { LastName = normalizedLastName };
            changed = true;
        }

        if (birthDate.HasValue && state.BirthDate != birthDate) {
            state = state with { BirthDate = birthDate };
            changed = true;
        }

        if (gender is not null) {
            var normalizedGender = NormalizeRequiredGender(gender, nameof(gender));
            if (state.Gender != normalizedGender) {
                state = state with { Gender = normalizedGender };
                changed = true;
            }
        }

        if (weight.HasValue && !NullableAreClose(state.Weight, weight.Value)) {
            state = state with { Weight = weight };
            changed = true;
        }

        if (height.HasValue && !NullableAreClose(state.Height, height.Value)) {
            state = state with { Height = height };
            changed = true;
        }

        if (changed) {
            ApplyPersonalProfileState(state);
        }

        return changed;
    }

    private bool ApplyActivityChanges(
        ActivityLevel? activityLevel,
        int? stepGoal,
        double? hydrationGoal) {
        var updatedActivityGoals = GetActivityGoals().With(
            stepGoal: stepGoal,
            hydrationGoal: hydrationGoal);
        var state = GetPersonalProfileState();

        var changed = false;

        if (activityLevel.HasValue && state.ActivityLevel != activityLevel.Value) {
            state = state with { ActivityLevel = activityLevel.Value };
            changed = true;
        }

        if (StepGoal != updatedActivityGoals.StepGoal || !NullableAreClose(HydrationGoal, updatedActivityGoals.HydrationGoal)) {
            ApplyActivityGoals(updatedActivityGoals);
            changed = true;
        }

        if (changed) {
            ApplyPersonalProfileState(state);
        }

        return changed;
    }

    private bool ApplyProfileMediaChanges(string? profileImage, ImageAssetId? profileImageAssetId) {
        var normalizedProfileImage = NormalizeOptionalProfileText(profileImage);
        var state = GetProfileMediaState();
        var changed = false;

        if (profileImage is not null && state.ProfileImage != normalizedProfileImage) {
            state = state with { ProfileImage = normalizedProfileImage };
            changed = true;
        }

        if (profileImageAssetId.HasValue && state.ProfileImageAssetId != profileImageAssetId) {
            state = state with { ProfileImageAssetId = profileImageAssetId };
            changed = true;
        }

        if (changed) {
            ApplyProfileMediaState(state);
        }

        return changed;
    }

    private bool ApplyPreferencesChanges(
        string? dashboardLayoutJson,
        string? language,
        string? theme,
        string? uiStyle,
        bool? pushNotificationsEnabled,
        bool? fastingPushNotificationsEnabled,
        bool? socialPushNotificationsEnabled,
        int? fastingCheckInReminderHours,
        int? fastingCheckInFollowUpReminderHours) {
        var normalizedDashboardLayoutJson = NormalizeOptionalProfileText(dashboardLayoutJson);
        var normalizedLanguage = NormalizeOptionalLanguage(language, nameof(language));
        var normalizedTheme = NormalizeOptionalTheme(theme, nameof(theme));
        var normalizedUiStyle = NormalizeOptionalUiStyle(uiStyle, nameof(uiStyle));
        var state = GetPreferenceState();

        EnsureLanguage(language, nameof(language));
        EnsureTheme(theme, nameof(theme));
        EnsureUiStyle(uiStyle, nameof(uiStyle));
        EnsureReminderHours(fastingCheckInReminderHours, nameof(fastingCheckInReminderHours));
        EnsureReminderHours(fastingCheckInFollowUpReminderHours, nameof(fastingCheckInFollowUpReminderHours));

        var changed = false;

        if (dashboardLayoutJson is not null && state.DashboardLayoutJson != normalizedDashboardLayoutJson) {
            state = state with { DashboardLayoutJson = normalizedDashboardLayoutJson };
            changed = true;
        }

        if (language is not null && state.Language != normalizedLanguage) {
            state = state with { Language = normalizedLanguage };
            changed = true;
        }

        if (theme is not null && state.Theme != normalizedTheme) {
            state = state with { Theme = normalizedTheme };
            changed = true;
        }

        if (uiStyle is not null && state.UiStyle != normalizedUiStyle) {
            state = state with { UiStyle = normalizedUiStyle };
            changed = true;
        }

        if (pushNotificationsEnabled.HasValue && state.PushNotificationsEnabled != pushNotificationsEnabled.Value) {
            state = state with { PushNotificationsEnabled = pushNotificationsEnabled.Value };
            changed = true;
        }

        if (fastingPushNotificationsEnabled.HasValue && state.FastingPushNotificationsEnabled != fastingPushNotificationsEnabled.Value) {
            state = state with { FastingPushNotificationsEnabled = fastingPushNotificationsEnabled.Value };
            changed = true;
        }

        if (socialPushNotificationsEnabled.HasValue && state.SocialPushNotificationsEnabled != socialPushNotificationsEnabled.Value) {
            state = state with { SocialPushNotificationsEnabled = socialPushNotificationsEnabled.Value };
            changed = true;
        }

        if (fastingCheckInReminderHours.HasValue && state.FastingCheckInReminderHours != fastingCheckInReminderHours.Value) {
            state = state with { FastingCheckInReminderHours = fastingCheckInReminderHours.Value };
            changed = true;
        }

        if (fastingCheckInFollowUpReminderHours.HasValue &&
            state.FastingCheckInFollowUpReminderHours != fastingCheckInFollowUpReminderHours.Value) {
            state = state with { FastingCheckInFollowUpReminderHours = fastingCheckInFollowUpReminderHours.Value };
            changed = true;
        }

        if (state.FastingCheckInFollowUpReminderHours <= state.FastingCheckInReminderHours) {
            throw new ArgumentOutOfRangeException(
                nameof(fastingCheckInFollowUpReminderHours),
                "Follow-up reminder hour must be greater than the first reminder hour.");
        }

        if (changed) {
            ApplyPreferenceState(state);
        }

        return changed;
    }

    private static void EnsureReminderHours(int? value, string paramName) {
        if (!value.HasValue) {
            return;
        }

        if (value.Value is < 1 or > 168) {
            throw new ArgumentOutOfRangeException(paramName, "Reminder hour must be between 1 and 168.");
        }
    }
}
