using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Meals.Mappings;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Application.Meals.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Commands.UpdateMeal;

public sealed class UpdateMealCommandHandler(
    IMealReadRepository mealReadRepository,
    IMealWriteRepository mealWriteRepository,
    IMealNutritionService mealNutritionService,
    IRecentItemUsageRecorder recentItemUsageRecorder,
    IImageAssetCleanupService imageAssetCleanupService,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider dateTimeProvider,
    IImageAssetAccessService imageAssetAccessService)
    : ICommandHandler<UpdateMealCommand, Result<MealModel>> {
    public async Task<Result<MealModel>> Handle(UpdateMealCommand command, CancellationToken cancellationToken) {
        Result<UpdateMealValues> valuesResult = await UpdateMealValuePreparer.PrepareAsync(
            command,
            mealReadRepository,
            currentUserAccessService,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<MealModel>(valuesResult.Error);
        }

        UpdateMealValues values = valuesResult.Value;
        Result updateResult = await UpdateMealApplier.ApplyAsync(
            values.Meal,
            command,
            values,
            mealNutritionService,
            imageAssetAccessService,
            dateTimeProvider,
            cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure) {
            return Result.Failure<MealModel>(updateResult.Error);
        }

        await mealWriteRepository.UpdateAsync(values.Meal, cancellationToken).ConfigureAwait(false);
        await recentItemUsageRecorder.RegisterUsageAsync(
            values.UserId,
            values.Meal.Items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToList(),
            values.Meal.Items.Where(x => x.RecipeId.HasValue).Select(x => x.RecipeId!.Value).ToList(),
            cancellationToken).ConfigureAwait(false);

        await UpdateMealImageCleanup.DeleteOldImageAssetAsync(
            command,
            values.OldAssetId,
            imageAssetCleanupService,
            cancellationToken).ConfigureAwait(false);
        return await LoadUpdatedAsync(values.Meal.Id, values.UserId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<MealModel>> LoadUpdatedAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken) {
        Meal? updated = await mealReadRepository.GetByIdAsync(
            mealId,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return updated is null
            ? Result.Failure<MealModel>(Errors.Meal.InvalidData("Failed to load updated meal."))
            : Result.Success(updated.ToModel());
    }

}
