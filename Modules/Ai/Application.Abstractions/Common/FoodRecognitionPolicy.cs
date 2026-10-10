using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public static class FoodRecognitionPolicy {
    public static bool RequiresNutrition(bool isProductLabel, FoodVisionModel vision) => !isProductLabel && vision.Items.Count > 0;

    public static bool IsValidVision(FoodVisionModel vision) => vision.Items?.All(item => item is not null &&
        !string.IsNullOrWhiteSpace(item.NameEn) && !string.IsNullOrWhiteSpace(item.Unit) && item.Amount > 0 &&
        IsProbability(item.Confidence) && IsOptionalProbability(item.CenterX) && IsOptionalProbability(item.CenterY) &&
        IsOptionalProbability(item.LocationConfidence)) is true;

    public static bool IsValidNutrition(FoodNutritionModel nutrition, int expectedItems) =>
        AreNonnegative(nutrition.Calories, nutrition.Protein, nutrition.Fat, nutrition.Carbs, nutrition.Fiber, nutrition.Alcohol) &&
        nutrition.Items is { } items && items.Count == expectedItems && items.All(item => item is not null &&
            !string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(item.Unit) && item.Amount > 0 &&
            AreNonnegative(item.Calories, item.Protein, item.Fat, item.Carbs, item.Fiber, item.Alcohol));

    private static bool IsProbability(decimal value) => value is >= 0 and <= 1;
    private static bool IsOptionalProbability(decimal? value) => value is null || IsProbability(value.Value);
    private static bool AreNonnegative(params decimal[] values) => values.All(value => value >= 0);
}
