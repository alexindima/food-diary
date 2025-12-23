using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IImageAssetRepository imageAssetRepository,
    IImageStorageService imageStorageService)
    : ICommandHandler<UpdateUserCommand, Result<UserResponse>> {
    public async Task<Result<UserResponse>> Handle(UpdateUserCommand command, CancellationToken cancellationToken) {
        var user = await userRepository.GetByIdAsync(command.UserId!.Value);
        if (user is null) {
            return Result.Failure<UserResponse>(User.NotFound(command.UserId.Value));
        }

        var activityLevelResult = ParseActivityLevel(command.ActivityLevel);
        if (activityLevelResult.IsFailure)
        {
            return Result.Failure<UserResponse>(activityLevelResult.Error);
        }

        var languageResult = NormalizeLanguage(command.Language);
        if (languageResult.IsFailure)
        {
            return Result.Failure<UserResponse>(languageResult.Error);
        }

        var oldAssetId = user.ProfileImageAssetId;
        ImageAssetId? newAssetId = null;
        if (command.ProfileImageAssetId.HasValue)
        {
            newAssetId = new ImageAssetId(command.ProfileImageAssetId.Value);
        }

        user.UpdateProfile(
            username: Normalize(command.Username),
            firstName: Normalize(command.FirstName),
            lastName: Normalize(command.LastName),
            birthDate: command.BirthDate,
            gender: Normalize(command.Gender),
            weight: command.Weight,
            height: command.Height,
            activityLevel: activityLevelResult.Value,
            dailyCalorieTarget: command.DailyCalorieTarget,
            proteinTarget: command.ProteinTarget,
            fatTarget: command.FatTarget,
            carbTarget: command.CarbTarget,
            fiberTarget: command.FiberTarget,
            stepGoal: command.StepGoal,
            waterGoal: command.WaterGoal,
            hydrationGoal: command.HydrationGoal,
            language: languageResult.Value,
            profileImage: Normalize(command.ProfileImage),
            profileImageAssetId: newAssetId,
            dashboardLayoutJson: command.DashboardLayoutJson
        );

        if (command.IsActive.HasValue) {
            if (command.IsActive.Value)
                user.Activate();
            else
                user.Deactivate();
        }

        await userRepository.UpdateAsync(user);

        if (oldAssetId.HasValue && (!newAssetId.HasValue || oldAssetId.Value.Value != newAssetId.Value.Value))
        {
            await TryDeleteAssetAsync(oldAssetId.Value, imageAssetRepository, imageStorageService, cancellationToken);
        }

        return Result.Success(user.ToResponse());
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result<ActivityLevel?> ParseActivityLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success<ActivityLevel?>(null);
        }

        return Enum.TryParse<ActivityLevel>(value, true, out var parsed)
            ? Result.Success<ActivityLevel?>(parsed)
            : Result.Failure<ActivityLevel?>(Validation.Invalid(nameof(UpdateUserCommand.ActivityLevel), "Invalid activity level value."));
    }

    private static Result<string?> NormalizeLanguage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success<string?>(null);
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "en" or "ru"
            ? Result.Success<string?>(normalized)
            : Result.Failure<string?>(Validation.Invalid(nameof(UpdateUserCommand.Language), "Invalid language value."));
    }

    private static async Task TryDeleteAssetAsync(
        ImageAssetId assetId,
        IImageAssetRepository imageAssetRepository,
        IImageStorageService storageService,
        CancellationToken cancellationToken)
    {
        var asset = await imageAssetRepository.GetByIdAsync(assetId, cancellationToken);
        if (asset is null)
        {
            return;
        }

        var inUse = await imageAssetRepository.IsAssetInUse(assetId, cancellationToken);
        if (inUse)
        {
            return;
        }

        await storageService.DeleteAsync(asset.ObjectKey, cancellationToken);
        await imageAssetRepository.DeleteAsync(asset, cancellationToken);
    }
}
