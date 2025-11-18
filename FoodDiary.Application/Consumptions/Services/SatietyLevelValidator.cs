using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Consumptions.Services;

public static class SatietyLevelValidator
{
    private const int MinLevel = 0;
    private const int MaxLevel = 9;

    public static Result Validate(int? preMealSatietyLevel, int? postMealSatietyLevel)
    {
        var preResult = ValidateLevel(nameof(preMealSatietyLevel), preMealSatietyLevel);
        if (preResult.IsFailure)
        {
            return preResult;
        }

        var postResult = ValidateLevel(nameof(postMealSatietyLevel), postMealSatietyLevel);
        return postResult.IsFailure ? postResult : Result.Success();
    }

    private static Result ValidateLevel(string fieldName, int? value)
    {
        if (value is null)
        {
            return Result.Success();
        }

        if (value is >= MinLevel and <= MaxLevel)
        {
            return Result.Success();
        }

        return Result.Failure(Errors.Validation.Invalid(fieldName, $"Satiety level must be between {MinLevel} and {MaxLevel}."));
    }
}
