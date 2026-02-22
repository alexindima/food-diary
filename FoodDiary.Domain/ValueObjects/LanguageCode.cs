namespace FoodDiary.Domain.ValueObjects;

public readonly record struct LanguageCode {
    private const string En = "en";
    private const string Ru = "ru";

    public string Value { get; }

    private LanguageCode(string value) {
        Value = value;
    }

    public static bool TryParse(string? value, out LanguageCode language) {
        if (string.IsNullOrWhiteSpace(value)) {
            language = default;
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized is En or Ru) {
            language = new LanguageCode(normalized);
            return true;
        }

        language = default;
        return false;
    }

    public static LanguageCode FromPreferred(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return new LanguageCode(En);
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized.StartsWith(Ru)
            ? new LanguageCode(Ru)
            : new LanguageCode(En);
    }

    public override string ToString() => Value;
}
