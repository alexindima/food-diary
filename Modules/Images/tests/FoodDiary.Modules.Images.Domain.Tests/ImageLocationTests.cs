using FoodDiary.Modules.Images.Domain.ValueObjects;

namespace FoodDiary.Modules.Images.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ImageLocationTests {
    [Fact]
    public void Locations_RetainProviderEncodingAndLegacyEmptyKeys() {
        const string signed = "https://storage.example/objects/a%2Fb?sig=X%2B%2F&expires=1";
        Assert.Equal(signed, SignedImageUploadUrl.FromProviderValue(signed).Value);
        Assert.Equal("relative/legacy-image", PublicImageUrl.FromProviderValue("relative/legacy-image").Value);
        Assert.Equal("", ObjectStorageKey.FromStoredValue("").Value);
        Assert.Equal(" key ", ObjectStorageKey.FromStoredValue(" key ").Value);
        Assert.Equal("key", ObjectStorageKey.FromInput(" key ").Value);
    }
}
