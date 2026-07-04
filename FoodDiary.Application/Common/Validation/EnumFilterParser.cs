namespace FoodDiary.Application.Common.Validation;

public static class EnumFilterParser {
    public static TEnum? ParseOptional<TEnum>(string? value)
        where TEnum : struct, Enum =>
        !string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
            ? parsed
            : null;

    public static TEnum[]? ParseMany<TEnum>(IEnumerable<string>? values)
        where TEnum : struct, Enum {
        TEnum[] parsed = [.. values?
            .Select(ParseOptional<TEnum>)
            .OfType<TEnum>()
            .Distinct() ?? []];

        return parsed.Length > 0 ? parsed : null;
    }
}
