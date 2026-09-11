namespace FoodDiary.Domain.ValueObjects;

public readonly record struct SurfaceStyleCode {
    public string Value { get; }

    private SurfaceStyleCode(string value) {
        Value = value;
    }

    public static bool TryParse(string? value, out SurfaceStyleCode style) {
        string? normalized = value?.Trim().ToLowerInvariant();
        if (normalized is "normal" or "matte" or "glass") {
            style = new SurfaceStyleCode(normalized);
            return true;
        }

        style = default;
        return false;
    }

    public static SurfaceStyleCode Default => new("normal");

    public override string ToString() => Value;
}
