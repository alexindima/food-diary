using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
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
        if (command.UserId is null || command.UserId.Value == UserId.Empty) {
            return Result.Failure<UserModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) {
            return Result.Failure<UserModel>(User.NotFound(userId));
        }

        var activityLevelResult = ParseActivityLevel(command.ActivityLevel);
        if (activityLevelResult.IsFailure) {
            return Result.Failure<UserModel>(activityLevelResult.Error);
        }

        var languageResult = NormalizeLanguage(command.Language);
        if (languageResult.IsFailure) {
            return Result.Failure<UserModel>(languageResult.Error);
        }

        var genderResult = ParseGender(command.Gender);
        if (genderResult.IsFailure) {
            return Result.Failure<UserModel>(genderResult.Error);
        }

        var oldAssetId = user.ProfileImageAssetId;
        ImageAssetId? newAssetId = null;
        if (command.ProfileImageAssetId.HasValue) {
            newAssetId = new ImageAssetId(command.ProfileImageAssetId.Value);
        }

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
            if (command.IsActive.Value)
                user.Activate();
            else
                user.Deactivate();
        }

        await userRepository.UpdateAsync(user, cancellationToken);

        if (oldAssetId.HasValue && (!newAssetId.HasValue || oldAssetId.Value.Value != newAssetId.Value.Value)) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken);
        }

        return Result.Success(user.ToModel());
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result<ActivityLevel?> ParseActivityLevel(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<ActivityLevel?>(null);
        }

        return Enum.TryParse<ActivityLevel>(value, true, out var parsed)
            ? Result.Success<ActivityLevel?>(parsed)
            : Result.Failure<ActivityLevel?>(Validation.Invalid(nameof(UpdateUserCommand.ActivityLevel), "Invalid activity level value."));
    }

    private static Result<string?> NormalizeLanguage(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<string?>(null);
        }

        return LanguageCode.TryParse(value, out var language)
            ? Result.Success<string?>(language.Value)
            : Result.Failure<string?>(Validation.Invalid(nameof(UpdateUserCommand.Language), "Invalid language value."));
    }

    private static Result<string?> ParseGender(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<string?>(null);
        }

        return GenderCode.TryParse(value, out var gender)
            ? Result.Success<string?>(gender.Value)
            : Result.Failure<string?>(Validation.Invalid(nameof(UpdateUserCommand.Gender), "Invalid gender value."));
    }

}
