using FoodDiary.Modules.Meals.Application.Mappings;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.RecentItems.Contracts.Common;

using FoodDiary.Modules.Meals.Service.Contracts.Models;
using FoodDiary.Modules.Meals.Application.Services;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.Meals.Application.Commands.UpdateMeal;

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
        // The command pipeline persists staged changes after this handler returns.
        return Result.Success(values.Meal.ToModel());
    }

}
