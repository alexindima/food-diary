namespace FoodDiary.Application.Admin.Internal.Validation;

internal static class EnumFilterParser {
    public static TEnum? ParseOptional<TEnum>(string? value)
        where TEnum : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && EnumValueParser.TryParse(value, out TEnum parsed)
            ? parsed
            : null;
}
