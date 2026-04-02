using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Users;

public sealed partial class User {
    public void SetLanguage(string language) {
        EnsureNotDeleted();
        if (ApplyPreferencesChanges(dashboardLayoutJson: null, language: language)) {
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

    public void UpdatePreferences(
        string? dashboardLayoutJson = null,
        string? language = null) {
        UpdatePreferences(new UserPreferenceUpdate(dashboardLayoutJson, language));
    }

    public void UpdatePreferences(UserPreferenceUpdate update) {
        EnsureNotDeleted();
        if (ApplyPreferencesChanges(update.DashboardLayoutJson, update.Language)) {
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

        var state = GetProfileState();
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
            ApplyProfileState(state);
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
        var state = GetProfileState();

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
            ApplyProfileState(state);
        }

        return changed;
    }

    private bool ApplyProfileMediaChanges(string? profileImage, ImageAssetId? profileImageAssetId) {
        var normalizedProfileImage = NormalizeOptionalProfileText(profileImage);
        var state = GetProfileState();
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
            ApplyProfileState(state);
        }

        return changed;
    }

    private bool ApplyPreferencesChanges(string? dashboardLayoutJson, string? language) {
        var normalizedDashboardLayoutJson = NormalizeOptionalProfileText(dashboardLayoutJson);
        var normalizedLanguage = NormalizeOptionalLanguage(language, nameof(language));
        var state = GetProfileState();

        EnsureLanguage(language, nameof(language));

        var changed = false;

        if (dashboardLayoutJson is not null && state.DashboardLayoutJson != normalizedDashboardLayoutJson) {
            state = state with { DashboardLayoutJson = normalizedDashboardLayoutJson };
            changed = true;
        }

        if (language is not null && state.Language != normalizedLanguage) {
            state = state with { Language = normalizedLanguage };
            changed = true;
        }

        if (changed) {
            ApplyProfileState(state);
        }

        return changed;
    }
}
