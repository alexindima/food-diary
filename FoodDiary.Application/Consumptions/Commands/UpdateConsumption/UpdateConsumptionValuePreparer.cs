using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.UpdateConsumption;

internal static class UpdateConsumptionValuePreparer {
    public static async Task<Result<UpdateConsumptionValues>> PrepareAsync(
        UpdateConsumptionCommand command,
        IMealReadRepository mealReadRepository,
        ICurrentUserAccessService currentUserAccessService,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) {
        Result commandValidation = ValidateCommand(command);
        if (commandValidation.IsFailure) {
            return Result.Failure<UpdateConsumptionValues>(commandValidation.Error);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<UpdateConsumptionValues>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        var consumptionId = new MealId(command.ConsumptionId);
        Meal? meal = await mealReadRepository.GetByIdAsync(
            consumptionId,
            userId,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (meal is null) {
            return Result.Failure<UpdateConsumptionValues>(Errors.Consumption.NotFound(command.ConsumptionId));
        }

        Result<MealType?> mealTypeResult = EnumValueParser.ParseOptional<MealType>(
            command.MealType,
            nameof(command.MealType),
            "Unknown meal type value.");
        if (mealTypeResult.IsFailure) {
            return Result.Failure<UpdateConsumptionValues>(mealTypeResult.Error);
        }

        ImageAssetId? oldAssetId = meal.ImageAssetId;
        Result<ImageAssetResolution> imageAssetResult = await ImageAssetResolver.ResolveOptionalAsync(
            command.ImageAssetId,
            nameof(command.ImageAssetId),
            userId,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<UpdateConsumptionValues>(imageAssetResult.Error);
        }

        return Result.Success(new UpdateConsumptionValues(
            userId,
            consumptionId,
            meal,
            mealTypeResult.Value,
            imageAssetResult.Value.ImageAssetId,
            imageAssetResult.Value.ImageAsset,
            oldAssetId));
    }

    private static Result ValidateCommand(UpdateConsumptionCommand command) {
        if (command.ConsumptionId == Guid.Empty) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(command.ConsumptionId), "Consumption id must not be empty."));
        }

        bool hasManualItems = command.Items is { Count: > 0 };
        bool hasAiItems = command.AiSessions is { Count: > 0 } && command.AiSessions.Any(session => session.Items.Count > 0);
        return hasManualItems || hasAiItems
            ? Result.Success()
            : Result.Failure(Errors.Validation.Required("Items"));
    }
}
