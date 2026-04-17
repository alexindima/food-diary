namespace FoodDiary.Domain.ValueObjects;

public readonly record struct ThemeCode {
    private const string Ocean = "ocean";
    private const string Leaf = "leaf";

    public string Value { get; }

    private ThemeCode(string value) {
        Value = value;
    }

    public static bool TryParse(string? value, out ThemeCode theme) {
        if (string.IsNullOrWhiteSpace(value)) {
            theme = default;
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized is Ocean or Leaf) {
            theme = new ThemeCode(normalized);
            return true;
        }

        theme = default;
        return false;
    }

    public static ThemeCode Default => new(Ocean);

    public override string ToString() => Value;
}
