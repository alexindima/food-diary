using FoodDiary.Application.Abstractions.Common.Validation;
namespace FoodDiary.Application.MealPlanning.Common.Validation;

internal static class EnumFilterParser {
    public static TEnum? ParseOptional<TEnum>(string? value)
        where TEnum : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && SharedEnumValueParser.TryParse(value, out TEnum parsed)
            ? parsed
            : null;
}
