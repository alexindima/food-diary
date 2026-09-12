using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    private const int DashboardLayoutJsonMaxLength = 65536;
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
        if (telegramUserId <= 0) {
            throw new ArgumentOutOfRangeException(nameof(telegramUserId));
        }
        if (TelegramUserId == telegramUserId) {
            return;
        }
        if (TelegramUserId.HasValue) {
            throw new InvalidOperationException("Disconnect the current Telegram identity before linking another one.");
        }
        ApplyAccountState(GetAccountState().WithTelegram(telegramUserId));
        AdvanceSecurityVersion();
        SetModified();
    }

    public void BindTelegramOidcIdentity(string issuer, string subject) {
        EnsureNotDeleted();
        if (!TelegramUserId.HasValue) {
            throw new InvalidOperationException("Link Telegram before binding its OIDC identity.");
        }
        string validatedIssuer = DomainGuard.RequiredText(issuer, 200, nameof(issuer));
        string validatedSubject = DomainGuard.RequiredText(subject, 255, nameof(subject));
        if (TelegramOidcIssuer is not null || TelegramOidcSubject is not null) {
            if (!string.Equals(TelegramOidcIssuer, validatedIssuer, StringComparison.Ordinal) ||
                !string.Equals(TelegramOidcSubject, validatedSubject, StringComparison.Ordinal)) {
                throw new InvalidOperationException("The Telegram OIDC identity does not match the linked identity.");
            }
            return;
        }
        TelegramOidcIssuer = validatedIssuer;
        TelegramOidcSubject = validatedSubject;
        SetModified();
    }

    public void UnlinkTelegram() {
        EnsureNotDeleted();
        if (!TelegramUserId.HasValue) {
            return;
        }
        bool hasPasswordLogin = HasPassword && Email is not null && IsEmailConfirmed;
        bool hasGoogleLogin = GoogleIssuer is not null && GoogleSubject is not null;
        if (!hasPasswordLogin && !hasGoogleLogin) {
            throw new InvalidOperationException("Add another sign-in method before disconnecting Telegram.");
        }
        ApplyAccountState(GetAccountState().WithTelegram(null));
        TelegramOidcIssuer = null;
        TelegramOidcSubject = null;
        AdvanceSecurityVersion();
        SetModified();
    }

    public void SetTimeZone(string timeZoneId) {
        EnsureNotDeleted();
        string normalized = DomainGuard.RequiredText(timeZoneId, 100, nameof(timeZoneId));
        var zone = TimeZoneInfo.FindSystemTimeZoneById(normalized);
        if (!string.Equals(zone.Id, "UTC", StringComparison.Ordinal) && !zone.HasIanaId) {
            throw new ArgumentException("An IANA time zone is required.", nameof(timeZoneId));
        }
        if (string.Equals(TimeZoneId, zone.Id, StringComparison.Ordinal)) {
            return;
        }
        TimeZoneId = zone.Id;
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
            WeightKg: weight,
            HeightCm: height));
    }

    public void UpdatePersonalInfo(UserPersonalInfoUpdate update) {
        EnsureNotDeleted();
        if (ApplyPersonalInfoChanges(
            update.Username,
            update.FirstName,
            update.LastName,
            update.BirthDate,
            update.Gender,
            update.WeightKg,
            update.HeightCm)) {
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
            update.FastingCheckInFollowUpReminderHours,
            update.SurfaceStyle)) {
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
        string? normalizedUsername = NormalizeOptionalProfileText(username);
        string? normalizedFirstName = NormalizeOptionalProfileText(firstName);
        string? normalizedLastName = NormalizeOptionalProfileText(lastName);
        DateTime? normalizedBirthDate = NormalizeOptionalUtcDate(birthDate);

        EnsureBirthDateIsNotFuture(normalizedBirthDate);
        EnsureProfileWeight(weight, nameof(weight));
        EnsureProfileHeight(height, nameof(height));

        UserPersonalProfileState state = GetPersonalProfileState();
        bool changed = false;

        if (username is not null && !string.Equals(state.Username, normalizedUsername, StringComparison.Ordinal)) {
            state = state with { Username = normalizedUsername };
            changed = true;
        }

        if (firstName is not null && !string.Equals(state.FirstName, normalizedFirstName, StringComparison.Ordinal)) {
            state = state with { FirstName = normalizedFirstName };
            changed = true;
        }

        if (lastName is not null && !string.Equals(state.LastName, normalizedLastName, StringComparison.Ordinal)) {
            state = state with { LastName = normalizedLastName };
            changed = true;
        }

        if (normalizedBirthDate.HasValue && state.BirthDate != normalizedBirthDate) {
            state = state with { BirthDate = normalizedBirthDate };
            changed = true;
        }

        if (gender is not null) {
            string normalizedGender = NormalizeRequiredGender(gender, nameof(gender));
            if (!string.Equals(state.Gender, normalizedGender, StringComparison.Ordinal)) {
                state = state with { Gender = normalizedGender };
                changed = true;
            }
        }

        if (weight.HasValue && !NullableAreClose(state.WeightKg, weight.Value)) {
            state = state with { WeightKg = weight };
            changed = true;
        }

        if (height.HasValue && !NullableAreClose(state.HeightCm, height.Value)) {
            state = state with { HeightCm = height };
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
        EnsureActivityLevel(activityLevel, nameof(activityLevel));

        UserActivityGoals updatedActivityGoals = GetActivityGoals().With(
            stepGoal: stepGoal,
            hydrationGoal: hydrationGoal);
        UserPersonalProfileState state = GetPersonalProfileState();

        bool changed = false;

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
        string? normalizedProfileImage = NormalizeOptionalProfileText(profileImage);
        UserProfileMediaState state = GetProfileMediaState();
        bool changed = false;

        if (profileImage is not null && !string.Equals(state.ProfileImage, normalizedProfileImage, StringComparison.Ordinal)) {
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
        int? fastingCheckInFollowUpReminderHours,
        string? surfaceStyle = null) {
        UserPreferenceState state = GetPreferenceState();

        EnsureLanguage(language, nameof(language));
        EnsureTheme(theme, nameof(theme));
        EnsureUiStyle(uiStyle, nameof(uiStyle));
        EnsureReminderHours(fastingCheckInReminderHours, nameof(fastingCheckInReminderHours));
        EnsureReminderHours(fastingCheckInFollowUpReminderHours, nameof(fastingCheckInFollowUpReminderHours));

        UserPreferenceState nextState = ApplyPreferenceTextChanges(state, dashboardLayoutJson, language, theme, uiStyle);
        if (surfaceStyle is not null) {
            if (!SurfaceStyleCode.TryParse(surfaceStyle, out SurfaceStyleCode parsedSurface)) {
                throw new ArgumentOutOfRangeException(nameof(surfaceStyle), "Surface style must be one of the supported codes.");
            }

            nextState = nextState with { SurfaceStyle = parsedSurface.Value };
        }
        nextState = ApplyNotificationPreferenceChanges(
            nextState,
            pushNotificationsEnabled,
            fastingPushNotificationsEnabled,
            socialPushNotificationsEnabled);
        nextState = ApplyReminderPreferenceChanges(nextState, fastingCheckInReminderHours, fastingCheckInFollowUpReminderHours);

        if (nextState.FastingCheckInFollowUpReminderHours <= nextState.FastingCheckInReminderHours) {
            throw new ArgumentOutOfRangeException(
                nameof(fastingCheckInFollowUpReminderHours),
                "Follow-up reminder hour must be greater than the first reminder hour.");
        }

        if (nextState == state) {
            return false;
        }

        ApplyPreferenceState(nextState);
        return true;

    }

    private static UserPreferenceState ApplyPreferenceTextChanges(
        UserPreferenceState state,
        string? dashboardLayoutJson,
        string? language,
        string? theme,
        string? uiStyle) {
        state = ApplyStringPreference(
            state,
            dashboardLayoutJson,
            value => DomainGuard.OptionalJson(value, DashboardLayoutJsonMaxLength, nameof(dashboardLayoutJson)),
            static (current, value) => current with { DashboardLayoutJson = value });
        state = ApplyStringPreference(state, language, value => NormalizeOptionalLanguage(value!, nameof(language)), static (current, value) => current with { Language = value });
        state = ApplyStringPreference(state, theme, value => NormalizeOptionalTheme(value!, nameof(theme)), static (current, value) => current with { Theme = value });
        return ApplyStringPreference(state, uiStyle, value => NormalizeOptionalUiStyle(value!, nameof(uiStyle)), static (current, value) => current with { UiStyle = value });
    }

    private static UserPreferenceState ApplyNotificationPreferenceChanges(
        UserPreferenceState state,
        bool? pushNotificationsEnabled,
        bool? fastingPushNotificationsEnabled,
        bool? socialPushNotificationsEnabled) {
        state = pushNotificationsEnabled.HasValue
            ? state with { PushNotificationsEnabled = pushNotificationsEnabled.Value }
            : state;
        state = fastingPushNotificationsEnabled.HasValue
            ? state with { FastingPushNotificationsEnabled = fastingPushNotificationsEnabled.Value }
            : state;
        return socialPushNotificationsEnabled.HasValue
            ? state with { SocialPushNotificationsEnabled = socialPushNotificationsEnabled.Value }
            : state;
    }

    private static UserPreferenceState ApplyReminderPreferenceChanges(
        UserPreferenceState state,
        int? fastingCheckInReminderHours,
        int? fastingCheckInFollowUpReminderHours) {
        state = fastingCheckInReminderHours.HasValue
            ? state with { FastingCheckInReminderHours = fastingCheckInReminderHours.Value }
            : state;
        return fastingCheckInFollowUpReminderHours.HasValue
            ? state with { FastingCheckInFollowUpReminderHours = fastingCheckInFollowUpReminderHours.Value }
            : state;
    }

    private static UserPreferenceState ApplyStringPreference(
        UserPreferenceState state,
        string? value,
        Func<string?, string?> normalize,
        Func<UserPreferenceState, string?, UserPreferenceState> apply) {
        return value is null
            ? state
            : apply(state, normalize(value));
    }

    private static void EnsureReminderHours(int? value, string paramName) {
        switch (value) {
            case null:
                return;
            case < 1 or > 168:
                throw new ArgumentOutOfRangeException(paramName, "Reminder hour must be between 1 and 168.");
        }
    }
}
