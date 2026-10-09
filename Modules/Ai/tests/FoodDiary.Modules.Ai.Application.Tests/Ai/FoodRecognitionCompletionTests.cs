using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Tests.Ai;

[ExcludeFromCodeCoverage]
public sealed class FoodRecognitionCompletionTests {
    [Fact]
    public void VisionOnlyAndNutritionFailure_KeepSuccessfulVisionStatusWithoutInventingNutrition() {
        FoodRecognitionCompletion visionOnly = FoodRecognitionCompletion.WithoutNutrition;
        var partial = FoodRecognitionCompletion.NutritionFailed("Ai.QuotaExceeded");
        Assert.Multiple(
            () => Assert.Equal("Succeeded", visionOnly.Status),
            () => Assert.Null(visionOnly.Nutrition),
            () => Assert.Null(visionOnly.ErrorCode),
            () => Assert.Null(visionOnly.NutritionErrorCode),
            () => Assert.Equal("Succeeded", partial.Status),
            () => Assert.Null(partial.Nutrition),
            () => Assert.Null(partial.ErrorCode),
            () => Assert.Equal("Ai.QuotaExceeded", partial.NutritionErrorCode));
    }

    [Fact]
    public void VisionFailureAndNutritionSuccess_KeepTheirExistingStorageShape() {
        var failed = FoodRecognitionCompletion.VisionFailed("unrecognized-provider-error");
        var nutrition = new FoodNutritionModel(0, 0, 0, 0, 0, 0, Items: []);
        var completed = FoodRecognitionCompletion.WithNutrition(nutrition);
        Assert.Multiple(
            () => Assert.Equal("Failed", failed.Status),
            () => Assert.Equal("unrecognized-provider-error", failed.ErrorCode),
            () => Assert.Null(failed.NutritionErrorCode),
            () => Assert.Null(failed.Nutrition),
            () => Assert.Equal("Succeeded", completed.Status),
            () => Assert.Same(nutrition, completed.Nutrition),
            () => Assert.Null(completed.ErrorCode),
            () => Assert.Null(completed.NutritionErrorCode));
    }

    [Fact]
    public void Construction_RejectsAbsentRequiredValuesAndRetainsExistingOpenErrorVocabulary() {
        Assert.Throws<ArgumentNullException>(() => FoodRecognitionCompletion.WithNutrition(null!));
        Assert.Throws<ArgumentNullException>(() => FoodRecognitionCompletion.VisionFailed(null!));
        Assert.Throws<ArgumentNullException>(() => FoodRecognitionCompletion.NutritionFailed(null!));
        Assert.Equal(string.Empty, FoodRecognitionCompletion.VisionFailed(string.Empty).ErrorCode);
        Assert.Equal(string.Empty, FoodRecognitionCompletion.NutritionFailed(string.Empty).NutritionErrorCode);
    }
}
