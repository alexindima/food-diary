namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UiStyleCode {
    private const string Classic = "classic";
    private const string Modern = "modern";

    public string Value { get; }

    private UiStyleCode(string value) {
        Value = value;
    }

    public static bool TryParse(string? value, out UiStyleCode style) {
        if (string.IsNullOrWhiteSpace(value)) {
            style = default;
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized is Classic or Modern) {
            style = new UiStyleCode(normalized);
            return true;
        }

        style = default;
        return false;
    }

    public static UiStyleCode Default => new(Classic);

    public override string ToString() => Value;
}
