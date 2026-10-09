using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public sealed record FoodRecognitionCompletion {
    public FoodNutritionModel? Nutrition { get; }
    public string? ErrorCode { get; }
    public string? NutritionErrorCode { get; }
    public string Status => ErrorCode is null ? "Succeeded" : "Failed";

    private FoodRecognitionCompletion(FoodNutritionModel? nutrition, string? errorCode, string? nutritionErrorCode) {
        Nutrition = nutrition;
        ErrorCode = errorCode;
        NutritionErrorCode = nutritionErrorCode;
    }

    public static FoodRecognitionCompletion WithoutNutrition { get; } = new(nutrition: null, errorCode: null, nutritionErrorCode: null);

    public static FoodRecognitionCompletion VisionFailed(string errorCode) {
        ArgumentNullException.ThrowIfNull(errorCode);
        return new FoodRecognitionCompletion(nutrition: null, errorCode, nutritionErrorCode: null);
    }

    public static FoodRecognitionCompletion NutritionFailed(string errorCode) {
        ArgumentNullException.ThrowIfNull(errorCode);
        return new FoodRecognitionCompletion(nutrition: null, errorCode: null, nutritionErrorCode: errorCode);
    }

    public static FoodRecognitionCompletion WithNutrition(FoodNutritionModel nutrition) {
        ArgumentNullException.ThrowIfNull(nutrition);
        return new FoodRecognitionCompletion(nutrition, errorCode: null, nutritionErrorCode: null);
    }
}
