namespace FoodDiary.Modules.Images.Domain.ValueObjects;

public sealed record SignedImageUploadUrl {
    public string Value { get; }
    private SignedImageUploadUrl(string value) => Value = value;
    public static SignedImageUploadUrl FromProviderValue(string value) => new(value);
}
