using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Text.Json;
using FoodDiary.Domain.Entities.Assets;

namespace FoodDiary.Application.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(
    IUserContextService userContextService,
    IImageAssetCleanupService imageAssetCleanupService,
    IImageAssetAccessService imageAssetAccessService)
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
        await CleanupOldProfileImageAssetAsync(oldAssetId, values.ProfileImageAssetId, cancellationToken).ConfigureAwait(false);

        return Result.Success(values.User.ToModel());
    }

    private async Task<Result<UpdateUserValues>> PrepareUpdateValuesAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<UpdateUserValues>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
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
        Result<ActivityLevel?> activityLevelResult = EnumValueParser.ParseOptional<ActivityLevel>(
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

        Result<string?> themeResult = UserPreferenceCodeParser.ParseOptionalTheme(
            command.Theme,
            nameof(UpdateUserCommand.Theme),
            "Invalid theme value.");
        if (themeResult.IsFailure) {
            return Result.Failure<ParsedUserPreferences>(themeResult.Error);
        }

        Result<string?> uiStyleResult = UserPreferenceCodeParser.ParseOptionalUiStyle(
            command.UiStyle,
            nameof(UpdateUserCommand.UiStyle),
            "Invalid UI style value.");
        if (uiStyleResult.IsFailure) {
            return Result.Failure<ParsedUserPreferences>(uiStyleResult.Error);
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
            themeResult.Value,
            uiStyleResult.Value,
            genderResult.Value));
    }

    private async Task<Result<ProfileImageValues>> ResolveProfileImageAsync(
        UpdateUserCommand command,
        UserId userId,
        CancellationToken cancellationToken) {
        Result<ImageAssetId?> profileImageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ProfileImageAssetId, nameof(command.ProfileImageAssetId));
        if (profileImageAssetIdResult.IsFailure) {
            return Result.Failure<ProfileImageValues>(profileImageAssetIdResult.Error);
        }

        ImageAssetId? newAssetId = profileImageAssetIdResult.Value;
        Result<ImageAsset?> profileImageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            newAssetId,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (profileImageAssetResult.IsFailure) {
            return Result.Failure<ProfileImageValues>(profileImageAssetResult.Error);
        }

        return Result.Success(new ProfileImageValues(
            newAssetId,
            profileImageAssetResult.Value?.Url ?? Normalize(command.ProfileImage)));
    }

    private static void ApplyUpdates(User user, UpdateUserCommand command, UpdateUserValues values) {
        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            Username: Normalize(command.Username),
            FirstName: Normalize(command.FirstName),
            LastName: Normalize(command.LastName),
            BirthDate: command.BirthDate,
            Gender: values.Gender,
            Weight: command.Weight,
            Height: command.Height));
        user.UpdateActivity(new UserActivityUpdate(
            ActivityLevel: values.ActivityLevel,
            StepGoal: command.StepGoal,
            HydrationGoal: command.HydrationGoal));
        user.UpdatePreferences(new UserPreferenceUpdate(
            DashboardLayoutJson: values.DashboardLayoutJson,
            Language: values.Language,
            Theme: values.Theme,
            UiStyle: values.UiStyle,
            PushNotificationsEnabled: command.PushNotificationsEnabled,
            FastingPushNotificationsEnabled: command.FastingPushNotificationsEnabled,
            SocialPushNotificationsEnabled: command.SocialPushNotificationsEnabled));
        user.UpdateProfileMedia(new UserProfileMediaUpdate(
            ProfileImage: values.ProfileImage,
            ProfileImageAssetId: values.ProfileImageAssetId));

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
            await imageAssetCleanupService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

}
