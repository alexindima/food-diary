using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Consumptions.Services;

public static class SatietyLevelValidator {
    private const int MinLevel = 0;
    private const int MaxLevel = 9;
    private const string PreMealSatietyField = "PreMealSatietyLevel";
    private const string PostMealSatietyField = "PostMealSatietyLevel";

    public static Result Validate(int? preMealSatietyLevel, int? postMealSatietyLevel) {
        var preResult = ValidateLevel(PreMealSatietyField, preMealSatietyLevel);
        if (preResult.IsFailure) {
            return preResult;
        }

        var postResult = ValidateLevel(PostMealSatietyField, postMealSatietyLevel);
        return postResult.IsFailure ? postResult : Result.Success();
    }

    private static Result ValidateLevel(string fieldName, int? value) {
        return value is null or >= MinLevel and <= MaxLevel
            ? Result.Success()
            : Result.Failure(Errors.Validation.Invalid(fieldName, $"Satiety level must be between {MinLevel} and {MaxLevel}."));
    }
}
