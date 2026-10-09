namespace FoodDiary.Modules.Images.Domain.ValueObjects;

public sealed record PublicImageUrl {
    public string Value { get; }
    private PublicImageUrl(string value) => Value = value;

    public static PublicImageUrl FromInput(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new PublicImageUrl(value.Trim());
    }

    public static PublicImageUrl FromProviderValue(string value) => new(value);
}
