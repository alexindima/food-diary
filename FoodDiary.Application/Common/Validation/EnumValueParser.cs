using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Common.Validation;

public static class EnumValueParser {
    public static Result<TEnum?> ParseOptional<TEnum>(string? value, string fieldName, string message)
        where TEnum : struct, Enum {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<TEnum?>(value: null);
        }

        return Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
            ? Result.Success<TEnum?>(parsed)
            : Result.Failure<TEnum?>(Errors.Validation.Invalid(fieldName, message));
    }

    public static Result<TEnum> ParseRequired<TEnum>(string? value, string fieldName, string message)
        where TEnum : struct, Enum {
        return Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
            ? Result.Success(parsed)
            : Result.Failure<TEnum>(Errors.Validation.Invalid(fieldName, message));
    }

    public static Result<TEnum> ParseRequired<TEnum>(string? value, Error error)
        where TEnum : struct, Enum {
        return Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
            ? Result.Success(parsed)
            : Result.Failure<TEnum>(error);
    }
}
