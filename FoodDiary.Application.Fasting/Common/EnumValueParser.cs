using FoodDiary.Results;

namespace FoodDiary.Application.Fasting.Common;

internal static class EnumValueParser {
    public static bool TryParse<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out parsed);

    public static bool CanParseOptional<TEnum>(string? value)
        where TEnum : struct, Enum =>
        string.IsNullOrWhiteSpace(value) || TryParse(value, out TEnum _);

    public static Result<TEnum> ParseRequired<TEnum>(string? value, Error error)
        where TEnum : struct, Enum =>
        TryParse(value, out TEnum parsed)
            ? Result.Success(parsed)
            : Result.Failure<TEnum>(error);
}
