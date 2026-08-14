using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Application.Products.Products.Common;

internal static class ProductEnumFilterParser {
    public static TEnum[]? ParseMany<TEnum>(IEnumerable<string>? values)
        where TEnum : struct, Enum {
        TEnum[] parsed = [.. values?
            .Select(ParseOptional<TEnum>)
            .OfType<TEnum>()
            .Distinct() ?? []];

        return parsed.Length > 0 ? parsed : null;
    }

    private static TEnum? ParseOptional<TEnum>(string? value)
        where TEnum : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && SharedEnumValueParser.TryParse(value, out TEnum parsed)
            ? parsed
            : null;
}
