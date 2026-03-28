using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Enums;
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
        if (user is null) {
            return Result.Failure<UserModel>(User.NotFound(userId));
        }

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

        var oldAssetId = user.ProfileImageAssetId;
        var newAssetId = profileImageAssetIdResult.Value;

        var dashboardLayoutJson = command.DashboardLayout is null
            ? null
            : JsonSerializer.Serialize(command.DashboardLayout);

        user.UpdatePersonalInfo(
            username: Normalize(command.Username),
            firstName: Normalize(command.FirstName),
            lastName: Normalize(command.LastName),
            birthDate: command.BirthDate,
            gender: genderResult.Value,
            weight: command.Weight,
            height: command.Height);
        user.UpdateActivity(
            activityLevel: activityLevelResult.Value,
            stepGoal: command.StepGoal,
            hydrationGoal: command.HydrationGoal);
        user.UpdatePreferences(
            language: languageResult.Value,
            dashboardLayoutJson: dashboardLayoutJson);
        user.UpdateProfileMedia(
            profileImage: Normalize(command.ProfileImage),
            profileImageAssetId: newAssetId);

        if (command.IsActive.HasValue) {
            user.SetActive(command.IsActive.Value);
        }

        await userRepository.UpdateAsync(user, cancellationToken);

        if (oldAssetId.HasValue && (!newAssetId.HasValue || oldAssetId.Value.Value != newAssetId.Value.Value)) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken);
        }

        return Result.Success(user.ToModel());
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

}
