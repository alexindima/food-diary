using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Text.Json;

namespace FoodDiary.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IImageAssetCleanupService imageAssetCleanupService)
    : ICommandHandler<UpdateUserCommand, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(UpdateUserCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<UserModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<UserModel>(accessError);
        }

        var currentUser = user!;

        var activityLevelResult = EnumValueParser.ParseOptional<ActivityLevel>(
            command.ActivityLevel,
            nameof(UpdateUserCommand.ActivityLevel),
            "Invalid activity level value.");
        if (activityLevelResult.IsFailure) {
            return Result.Failure<UserModel>(activityLevelResult.Error);
        }

        var languageResult = StringCodeParser.ParseOptionalLanguage(
            command.Language,
            nameof(UpdateUserCommand.Language),
            "Invalid language value.");
        if (languageResult.IsFailure) {
            return Result.Failure<UserModel>(languageResult.Error);
        }

        var profileImageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ProfileImageAssetId, nameof(command.ProfileImageAssetId));
        if (profileImageAssetIdResult.IsFailure) {
            return Result.Failure<UserModel>(profileImageAssetIdResult.Error);
        }

        var genderResult = StringCodeParser.ParseOptionalGender(
            command.Gender,
            nameof(UpdateUserCommand.Gender),
            "Invalid gender value.");
        if (genderResult.IsFailure) {
            return Result.Failure<UserModel>(genderResult.Error);
        }

        var oldAssetId = currentUser.ProfileImageAssetId;
        var newAssetId = profileImageAssetIdResult.Value;

        var dashboardLayoutJson = command.DashboardLayout is null
            ? null
            : JsonSerializer.Serialize(command.DashboardLayout);

        currentUser.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            Username: Normalize(command.Username),
            FirstName: Normalize(command.FirstName),
            LastName: Normalize(command.LastName),
            BirthDate: command.BirthDate,
            Gender: genderResult.Value,
            Weight: command.Weight,
            Height: command.Height));
        currentUser.UpdateActivity(new UserActivityUpdate(
            ActivityLevel: activityLevelResult.Value,
            StepGoal: command.StepGoal,
            HydrationGoal: command.HydrationGoal));
        currentUser.UpdatePreferences(new UserPreferenceUpdate(
            DashboardLayoutJson: dashboardLayoutJson,
            Language: languageResult.Value));
        currentUser.UpdateProfileMedia(new UserProfileMediaUpdate(
            ProfileImage: Normalize(command.ProfileImage),
            ProfileImageAssetId: newAssetId));

        if (command.IsActive.HasValue) {
            if (command.IsActive.Value) {
                currentUser.Activate();
            } else {
                currentUser.Deactivate();
            }
        }

        await userRepository.UpdateAsync(currentUser, cancellationToken);

        if (oldAssetId.HasValue && (!newAssetId.HasValue || oldAssetId.Value.Value != newAssetId.Value.Value)) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken);
        }

        return Result.Success(currentUser.ToModel());
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

}
