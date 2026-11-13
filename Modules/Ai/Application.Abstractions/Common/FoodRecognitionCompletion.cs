using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Domain.Recognition;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public closed record class FoodRecognitionCompletion {
    public FoodNutritionModel? Nutrition => this is NutritionAvailable available ? available.Value : null;
    public string? ErrorCode => this is VisionFailure failure ? failure.Code : null;
    public string? NutritionErrorCode => this is NutritionFailure failure ? failure.Code : null;
    public FoodRecognitionAction Action => this switch {
        VisionFailure => FoodRecognitionAction.Fail,
        VisionOnly or NutritionFailure or NutritionAvailable => FoodRecognitionAction.Complete,
    };
    public string Status => FoodRecognitionLifecycle.Encode(
        FoodRecognitionLifecycle.Next(FoodRecognitionStatus.Running, Action)!.Value);

    private FoodRecognitionCompletion() { }

    public static FoodRecognitionCompletion WithoutNutrition { get; } = new VisionOnly();

    public static FoodRecognitionCompletion VisionFailed(string errorCode) {
        ArgumentNullException.ThrowIfNull(errorCode);
        return new VisionFailure(errorCode);
    }

    public static FoodRecognitionCompletion NutritionFailed(string errorCode) {
        ArgumentNullException.ThrowIfNull(errorCode);
        return new NutritionFailure(errorCode);
    }

    public static FoodRecognitionCompletion WithNutrition(FoodNutritionModel nutrition) {
        ArgumentNullException.ThrowIfNull(nutrition);
        return new NutritionAvailable(nutrition);
    }

    private sealed record VisionOnly : FoodRecognitionCompletion;
    private sealed record VisionFailure(string Code) : FoodRecognitionCompletion;
    private sealed record NutritionFailure(string Code) : FoodRecognitionCompletion;
    private sealed record NutritionAvailable(FoodNutritionModel Value) : FoodRecognitionCompletion;
}
