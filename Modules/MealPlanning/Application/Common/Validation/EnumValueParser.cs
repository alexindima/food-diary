using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Application.MealPlanning.Common.Validation;

internal static class EnumValueParser {
    public static Result<TEnum?> ParseOptional<TEnum>(string? value, string fieldName, string message)
        where TEnum : struct, Enum {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<TEnum?>(value: null);
        }

        return SharedEnumValueParser.TryParse(value, out TEnum parsed)
            ? Result.Success<TEnum?>(parsed)
            : Result.Failure<TEnum?>(Errors.Validation.Invalid(fieldName, message));
    }
}
