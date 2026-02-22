namespace FoodDiary.Domain.ValueObjects;

public readonly record struct GenderCode {
    private const string Male = "M";
    private const string Female = "F";
    private const string Other = "O";

    public string Value { get; }

    private GenderCode(string value) {
        Value = value;
    }

    public static bool TryParse(string? value, out GenderCode gender) {
        if (string.IsNullOrWhiteSpace(value)) {
            gender = default;
            return false;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized is Male or Female or Other) {
            gender = new GenderCode(normalized);
            return true;
        }

        gender = default;
        return false;
    }

    public override string ToString() => Value;
}
