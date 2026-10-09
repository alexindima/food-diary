namespace FoodDiary.Domain.Primitives;

public static class FieldChanges {
    public static FieldChange<T> Unchanged<T>() where T : notnull => FieldChange<T>.Unchanged;
    public static FieldChange<T> Clear<T>() where T : notnull => FieldChange<T>.Clear;
    public static FieldChange<T> Set<T>(T value) where T : notnull => FieldChange<T>.Set(value);

    public static FieldChange<string> FromOptionalText(string? value, bool clear) {
        if (value is null) {
            return clear ? FieldChange<string>.Clear : FieldChange<string>.Unchanged;
        }
        if (clear) {
            return string.IsNullOrWhiteSpace(value)
                ? FieldChange<string>.Clear
                : throw new ArgumentException("A field cannot be set and cleared in the same change.", nameof(clear));
        }
        return FieldChange<string>.Set(value);
    }

    public static FieldChange<T> FromOptionalValue<T>(T? value, bool clear) where T : struct {
        if (value is null) {
            return clear ? FieldChange<T>.Clear : FieldChange<T>.Unchanged;
        }
        if (clear) {
            throw new ArgumentException("A field cannot be set and cleared in the same change.", nameof(clear));
        }
        return FieldChange<T>.Set(value.Value);
    }
}
