using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Common.Validation;

public static class EnumValueParser {
    public static bool TryParse<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out parsed);

    public static bool CanParse<TEnum>(string? value)
        where TEnum : struct, Enum =>
        TryParse<TEnum>(value, out _);

    public static bool CanParseOptional<TEnum>(string? value)
        where TEnum : struct, Enum =>
        string.IsNullOrWhiteSpace(value) || CanParse<TEnum>(value);

    public static bool CanParseDefined<TEnum>(string? value)
        where TEnum : struct, Enum =>
        TryParse(value, out TEnum parsed) && Enum.IsDefined(parsed);

    public static Result<TEnum?> ParseOptional<TEnum>(string? value, string fieldName, string message)
        where TEnum : struct, Enum {
        if (string.IsNullOrWhiteSpace(value)) {
            return Result.Success<TEnum?>(value: null);
        }

        return TryParse(value, out TEnum parsed)
            ? Result.Success<TEnum?>(parsed)
            : Result.Failure<TEnum?>(Errors.Validation.Invalid(fieldName, message));
    }

    public static Result<TEnum> ParseRequired<TEnum>(string? value, string fieldName, string message)
        where TEnum : struct, Enum {
        return TryParse(value, out TEnum parsed)
            ? Result.Success(parsed)
            : Result.Failure<TEnum>(Errors.Validation.Invalid(fieldName, message));
    }

    public static Result<TEnum> ParseRequired<TEnum>(string? value, Error error)
        where TEnum : struct, Enum {
        return TryParse(value, out TEnum parsed)
            ? Result.Success(parsed)
            : Result.Failure<TEnum>(error);
    }
}
