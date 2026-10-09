using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Application.Mappings;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Application.Common;

using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using System.Text.Json;

namespace FoodDiary.Modules.Users.Application.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(
    IUserContextService userContextService,
    IUserProfileImageService profileImageService)
    : ICommandHandler<UpdateUserCommand, Result<UserModel>> {
    private sealed record UpdateUserValues(
        User User,
        UserId UserId,
        ActivityLevel? ActivityLevel,
        string? Language,
        string? Theme,
        string? UiStyle,
        string? Gender,
        ImageAssetId? ProfileImageAssetId,
        string? ProfileImage,
        string? DashboardLayoutJson);

    private sealed record ParsedUserPreferences(
        ActivityLevel? ActivityLevel,
        string? Language,
        string? Theme,
        string? UiStyle,
        string? Gender);

    private sealed record ProfileImageValues(
        ImageAssetId? AssetId,
        string? Image);

    public async Task<Result<UserModel>> Handle(UpdateUserCommand command, CancellationToken cancellationToken) {
        Result<UpdateUserValues> valuesResult = await PrepareUpdateValuesAsync(command, cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<UserModel>(valuesResult.Error);
        }

        UpdateUserValues values = valuesResult.Value;
        ImageAssetId? oldAssetId = values.User.ProfileImageAssetId;
        ApplyUpdates(values.User, command, values);

        await userContextService.UpdateUserAsync(values.User, cancellationToken).ConfigureAwait(false);
        await CleanupOldProfileImageAssetAsync(oldAssetId, values.User.ProfileImageAssetId, cancellationToken).ConfigureAwait(false);

        return Result.Success(values.User.ToModel());
    }

    private async Task<Result<UpdateUserValues>> PrepareUpdateValuesAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            userContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<UpdateUserValues>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UpdateUserValues>(userResult.Error);
        }

        User currentUser = userResult.Value;
        Result<ParsedUserPreferences> preferencesResult = ParsePreferences(command);
        if (preferencesResult.IsFailure) {
            return Result.Failure<UpdateUserValues>(preferencesResult.Error);
        }

        Result<ProfileImageValues> profileImageResult = await ResolveProfileImageAsync(command, userId, cancellationToken).ConfigureAwait(false);
        if (profileImageResult.IsFailure) {
            return Result.Failure<UpdateUserValues>(profileImageResult.Error);
        }

        string? dashboardLayoutJson = command.DashboardLayout is null
            ? null
            : JsonSerializer.Serialize(command.DashboardLayout);

        ParsedUserPreferences preferences = preferencesResult.Value;
        return Result.Success(new UpdateUserValues(
            currentUser,
            userId,
            preferences.ActivityLevel,
            preferences.Language,
            preferences.Theme,
            preferences.UiStyle,
            preferences.Gender,
            profileImageResult.Value.AssetId,
            profileImageResult.Value.Image,
            dashboardLayoutJson));
    }

    private static Result<ParsedUserPreferences> ParsePreferences(UpdateUserCommand command) {
        if (!UserTimeZoneInput.IsValid(command.TimeZoneId)) {
            return Result.Failure<ParsedUserPreferences>(new Error("Validation.Invalid", "TimeZoneId must be a valid IANA time zone or UTC.", ErrorKind.Validation));
        }
        Result<ActivityLevel?> activityLevelResult = UserInputParser.ParseOptionalEnum<ActivityLevel>(
            command.ActivityLevel,
            nameof(UpdateUserCommand.ActivityLevel),
            "Invalid activity level value.");
        if (activityLevelResult.IsFailure) {
            return Result.Failure<ParsedUserPreferences>(activityLevelResult.Error);
        }

        Result<string?> languageResult = UserPreferenceCodeParser.ParseOptionalLanguage(
            command.Language,
            nameof(UpdateUserCommand.Language),
            "Invalid language value.");
        if (languageResult.IsFailure) {
            return Result.Failure<ParsedUserPreferences>(languageResult.Error);
        }

        Result<UserAppearancePreferences> appearancePreferencesResult = UserAppearancePreferencesParser.ParseOptional(
            command.Theme,
            command.UiStyle);
        if (appearancePreferencesResult.IsFailure) {
            return Result.Failure<ParsedUserPreferences>(appearancePreferencesResult.Error);
        }

        Result<string?> genderResult = UserPreferenceCodeParser.ParseOptionalGender(
            command.Gender,
            nameof(UpdateUserCommand.Gender),
            "Invalid gender value.");
        if (genderResult.IsFailure) {
            return Result.Failure<ParsedUserPreferences>(genderResult.Error);
        }

        return Result.Success(new ParsedUserPreferences(
            activityLevelResult.Value,
            languageResult.Value,
            appearancePreferencesResult.Value.Theme,
            appearancePreferencesResult.Value.UiStyle,
            genderResult.Value));
    }

    private async Task<Result<ProfileImageValues>> ResolveProfileImageAsync(
        UpdateUserCommand command,
        UserId userId,
        CancellationToken cancellationToken) {
        Result<ImageAssetId?> profileImageAssetIdResult = UserInputParser.ParseOptionalImageAssetId(command.ProfileImageAssetId, nameof(command.ProfileImageAssetId));
        if (profileImageAssetIdResult.IsFailure) {
            return Result.Failure<ProfileImageValues>(profileImageAssetIdResult.Error);
        }

        ImageAssetId? newAssetId = profileImageAssetIdResult.Value;
        Result<string?> profileImageAssetResult = await profileImageService.ResolveOptionalUrlAsync(
            newAssetId,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (profileImageAssetResult.IsFailure) {
            return Result.Failure<ProfileImageValues>(profileImageAssetResult.Error);
        }

        return Result.Success(new ProfileImageValues(
            newAssetId,
            profileImageAssetResult.Value ?? Normalize(command.ProfileImage)));
    }

    private static void ApplyUpdates(User user, UpdateUserCommand command, UpdateUserValues values) {
        FieldChange<ProfileBirthDate> birthDateChange = FieldChanges.Unchanged<ProfileBirthDate>();
        if (command.BirthDate is { } birthDate) {
            birthDateChange = FieldChanges.Set(ProfileBirthDate.FromEncodedDateTime(birthDate));
        } else if (command.BirthDateSpecified) {
            birthDateChange = FieldChanges.Clear<ProfileBirthDate>();
        }
        if (command.TimeZoneId is not null) {
            user.SetTimeZone(command.TimeZoneId);
        }
        user.UpdatePersonalInfoChanges(new UserPersonalInfoChanges(
            Username: command.Username?.Trim(),
            FirstName: command.FirstName?.Trim(),
            LastName: command.LastName?.Trim(),
            BirthDate: birthDateChange,
            Gender: values.Gender,
            WeightKg: command.WeightKg is { } weight ? ProfileWeightKg.Create(weight) : null,
            HeightCm: command.HeightCm is { } height ? ProfileHeightCm.Create(height) : null));
        user.UpdateActivity(new UserActivityUpdate(
            ActivityLevel: values.ActivityLevel,
            StepGoal: command.StepGoal,
            HydrationGoal: command.HydrationGoal));
        user.UpdatePreferences(new UserPreferenceUpdate(
            ReminderDelays: default,
            DashboardLayoutJson: values.DashboardLayoutJson,
            Language: values.Language,
            Theme: values.Theme,
            UiStyle: values.UiStyle,
            PushNotificationsEnabled: command.PushNotificationsEnabled,
            FastingPushNotificationsEnabled: command.FastingPushNotificationsEnabled,
            SocialPushNotificationsEnabled: command.SocialPushNotificationsEnabled));
        user.UpdateProfileMedia(new UserProfileMediaUpdate(
            ProfileImage: values.ProfileImage,
            ProfileImageAssetId: values.ProfileImageAssetId,
            ProfileImageSpecified: command.ProfileImageSpecified));

        if (command.IsActive.HasValue) {
            if (command.IsActive.Value) {
                user.Activate();
            } else {
                user.Deactivate();
            }
        }
    }

    private async Task CleanupOldProfileImageAssetAsync(
        ImageAssetId? oldAssetId,
        ImageAssetId? newAssetId,
        CancellationToken cancellationToken) {
        if (oldAssetId.HasValue && (!newAssetId.HasValue || oldAssetId.Value.Value != newAssetId.Value.Value)) {
            await profileImageService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

}
