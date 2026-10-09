namespace FoodDiary.Modules.Images.Domain.ValueObjects;

public sealed record ObjectStorageKey {
    public string Value { get; }
    private ObjectStorageKey(string value) => Value = value;

    public static ObjectStorageKey FromInput(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new ObjectStorageKey(value.Trim());
    }

    public static ObjectStorageKey FromStoredValue(string value) => new(value);
}
