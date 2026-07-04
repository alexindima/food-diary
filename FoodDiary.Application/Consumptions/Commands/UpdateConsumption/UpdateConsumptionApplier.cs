using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Consumptions.Commands.UpdateConsumption;

internal static class UpdateConsumptionApplier {
    public static async Task<Result> ApplyAsync(
        Meal meal,
        UpdateConsumptionCommand command,
        UpdateConsumptionValues values,
        IMealNutritionService mealNutritionService,
        IImageAssetAccessService imageAssetAccessService,
        TimeProvider dateTimeProvider,
        CancellationToken cancellationToken) {
        meal.UpdateDate(command.Date);
        meal.UpdateMealType(values.MealType);
        meal.UpdateComment(command.Comment);
        meal.UpdateImage(values.ImageAsset?.Url ?? command.ImageUrl, values.ImageAssetId);

        Result satietyValidation = SatietyLevelValidator.Validate(
            command.PreMealSatietyLevel,
            command.PostMealSatietyLevel);

        if (satietyValidation.IsFailure) {
            return satietyValidation;
        }

        meal.UpdateSatietyLevels(command.PreMealSatietyLevel, command.PostMealSatietyLevel);
        meal.ClearItems();
        meal.ClearAiSessions();

        Result itemsResult = ConsumptionManualItemAppender.Add(meal, command.Items);
        if (itemsResult.IsFailure) {
            return itemsResult;
        }

        Result aiSessionsResult = await ConsumptionAiSessionAppender.AddAsync(
            meal,
            command.AiSessions,
            values.UserId,
            imageAssetAccessService,
            dateTimeProvider,
            cancellationToken).ConfigureAwait(false);
        if (aiSessionsResult.IsFailure) {
            return aiSessionsResult;
        }

        return await ConsumptionNutritionApplier.ApplyAsync(
            meal,
            values.UserId,
            mealNutritionService,
            CreateNutritionInput(command),
            cancellationToken).ConfigureAwait(false);
    }

    private static ConsumptionNutritionInput CreateNutritionInput(UpdateConsumptionCommand command) =>
        new(
            command.IsNutritionAutoCalculated,
            command.ManualCalories,
            command.ManualProteins,
            command.ManualFats,
            command.ManualCarbs,
            command.ManualFiber,
            command.ManualAlcohol);
}
