namespace FoodDiary.Application.MealPlanning.Common.Validation;

internal static class EnumFilterParser {
    public static TEnum? ParseOptional<TEnum>(string? value)
        where TEnum : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
            ? parsed
            : null;
}
