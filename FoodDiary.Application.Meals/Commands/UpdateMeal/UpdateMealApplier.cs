using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Meals.Services;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Meals.Commands.UpdateMeal;

internal static class UpdateMealApplier {
    public static async Task<Result> ApplyAsync(
        Meal meal,
        UpdateMealCommand command,
        UpdateMealValues values,
        IMealNutritionService mealNutritionService,
        IImageAssetAccessService imageAssetAccessService,
        TimeProvider dateTimeProvider,
        CancellationToken cancellationToken) {
        meal.UpdateDate(command.Date);
        meal.UpdateMealType(values.MealType);
        meal.UpdateComment(command.Comment);
        string? imageUrl = values.ImageAsset?.Url ?? command.ImageUrl;
        meal.UpdateImage(
            imageUrl,
            clearImageUrl: imageUrl is null,
            imageAssetId: values.ImageAssetId,
            clearImageAssetId: values.ImageAssetId is null);

        Result satietyValidation = SatietyLevelValidator.Validate(
            command.PreMealSatietyLevel,
            command.PostMealSatietyLevel);

        if (satietyValidation.IsFailure) {
            return satietyValidation;
        }

        meal.UpdateSatietyLevels(command.PreMealSatietyLevel, command.PostMealSatietyLevel);
        meal.ClearItems();
        meal.ClearAiSessions();

        Result itemsResult = MealManualItemAppender.Add(meal, command.Items);
        if (itemsResult.IsFailure) {
            return itemsResult;
        }

        Result aiSessionsResult = await MealAiSessionAppender.AddAsync(
            meal,
            command.AiSessions,
            values.UserId,
            imageAssetAccessService,
            dateTimeProvider,
            cancellationToken).ConfigureAwait(false);
        if (aiSessionsResult.IsFailure) {
            return aiSessionsResult;
        }

        return await MealNutritionApplier.ApplyAsync(
            meal,
            values.UserId,
            mealNutritionService,
            CreateNutritionInput(command),
            cancellationToken).ConfigureAwait(false);
    }

    private static MealNutritionInput CreateNutritionInput(UpdateMealCommand command) =>
        new(
            command.IsNutritionAutoCalculated,
            command.ManualCalories,
            command.ManualProteins,
            command.ManualFats,
            command.ManualCarbs,
            command.ManualFiber,
            command.ManualAlcohol);
}
