using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Internal.Validation;

internal static class EnumValueParser {
    public static bool TryParse<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out parsed);

    public static bool CanParseDefined<TEnum>(string? value)
        where TEnum : struct, Enum =>
        TryParse(value, out TEnum parsed) && Enum.IsDefined(parsed);

    public static Result<TEnum> ParseRequired<TEnum>(string? value, string fieldName, string message)
        where TEnum : struct, Enum =>
        TryParse(value, out TEnum parsed)
            ? Result.Success(parsed)
            : Result.Failure<TEnum>(Errors.Validation.Invalid(fieldName, message));
}
