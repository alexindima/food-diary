namespace FoodDiary.Application.Exercises.Internal;

internal static class EnumValueParser {
    public static bool TryParse<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out parsed);
}
