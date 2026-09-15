using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Meals.Application.Common.Validation;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Commands.UpdateMeal;

internal static class UpdateMealValuePreparer {
    public static async Task<Result<UpdateMealValues>> PrepareAsync(
        UpdateMealCommand command,
        IMealReadRepository mealReadRepository,
        ICurrentUserAccessService currentUserAccessService,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) {
        Result<MealId> mealIdResult = ParseMealId(command);
        if (mealIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<UpdateMealValues, MealId>(mealIdResult);
        }

        Result itemsValidation = ValidateItems(command);
        if (itemsValidation.IsFailure) {
            return Result.Failure<UpdateMealValues>(itemsValidation.Error);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<UpdateMealValues>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        MealId mealId = mealIdResult.Value;
        Meal? meal = await mealReadRepository.GetByIdAsync(
            mealId,
            userId,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (meal is null) {
            return Result.Failure<UpdateMealValues>(MealErrors.NotFound(command.MealId));
        }

        Result<MealType?> mealTypeResult = EnumValueParser.ParseOptional<MealType>(
            command.MealType,
            nameof(command.MealType),
            "Unknown meal type value.");
        if (mealTypeResult.IsFailure) {
            return Result.Failure<UpdateMealValues>(mealTypeResult.Error);
        }

        ImageAssetId? oldAssetId = meal.ImageAssetId;
        Result<ImageAssetResolution> imageAssetResult = await ImageAssetResolver.ResolveOptionalAsync(
            command.ImageAssetId,
            nameof(command.ImageAssetId),
            userId,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<UpdateMealValues>(imageAssetResult.Error);
        }

        return Result.Success(new UpdateMealValues(
            userId,
            mealId,
            meal,
            mealTypeResult.Value,
            imageAssetResult.Value.ImageAssetId,
            imageAssetResult.Value.ImageAsset,
            oldAssetId));
    }

    private static Result<MealId> ParseMealId(UpdateMealCommand command) =>
        RequiredIdParser.Parse(
            command.MealId,
            nameof(command.MealId),
            "Meal id must not be empty.",
            value => new MealId(value));

    private static Result ValidateItems(UpdateMealCommand command) {
        bool hasManualItems = command.Items is { Count: > 0 };
        bool hasAiItems = command.AiSessions is { Count: > 0 } && command.AiSessions.Any(session => session.Items.Count > 0);
        return hasManualItems || hasAiItems
            ? Result.Success()
            : Result.Failure(Errors.Validation.Required("Items"));
    }
}
