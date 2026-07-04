using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.UpdateConsumption;

public sealed class UpdateConsumptionCommandHandler(
    IMealReadRepository mealReadRepository,
    IMealWriteRepository mealWriteRepository,
    IMealNutritionService mealNutritionService,
    IRecentItemWriteRepository recentItemRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider,
    IImageAssetAccessService imageAssetAccessService)
    : ICommandHandler<UpdateConsumptionCommand, Result<ConsumptionModel>> {
    public async Task<Result<ConsumptionModel>> Handle(UpdateConsumptionCommand command, CancellationToken cancellationToken) {
        Result<UpdateConsumptionValues> valuesResult = await UpdateConsumptionValuePreparer.PrepareAsync(
            command,
            mealReadRepository,
            currentUserAccessService,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(valuesResult.Error);
        }

        UpdateConsumptionValues values = valuesResult.Value;
        Result updateResult = await UpdateConsumptionApplier.ApplyAsync(
            values.Meal,
            command,
            values,
            mealNutritionService,
            imageAssetAccessService,
            dateTimeProvider,
            cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(updateResult.Error);
        }

        await mealWriteRepository.UpdateAsync(values.Meal, cancellationToken).ConfigureAwait(false);
        await recentItemRepository.RegisterUsageAsync(
            values.UserId,
            values.Meal.Items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToList(),
            values.Meal.Items.Where(x => x.RecipeId.HasValue).Select(x => x.RecipeId!.Value).ToList(),
            cancellationToken).ConfigureAwait(false);

        await UpdateConsumptionImageCleanup.DeleteOldImageAssetAsync(
            command,
            values.OldAssetId,
            imageAssetCleanupService,
            cancellationToken).ConfigureAwait(false);
        return await LoadUpdatedAsync(values.Meal.Id, values.UserId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<ConsumptionModel>> LoadUpdatedAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken) {
        Meal? updated = await mealReadRepository.GetByIdAsync(
            mealId,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return updated is null
            ? Result.Failure<ConsumptionModel>(Errors.Consumption.InvalidData("Failed to load updated consumption."))
            : Result.Success(updated.ToModel());
    }

}
