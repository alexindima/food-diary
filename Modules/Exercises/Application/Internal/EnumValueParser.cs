using FoodDiary.Application.Abstractions.Common.Validation;
namespace FoodDiary.Application.Exercises.Internal;

internal static class EnumValueParser {
    public static bool TryParse<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum =>
        SharedEnumValueParser.TryParse(value, out parsed);
}
