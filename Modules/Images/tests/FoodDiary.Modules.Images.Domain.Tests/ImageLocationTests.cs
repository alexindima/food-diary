using FoodDiary.Modules.Images.Domain.ValueObjects;

namespace FoodDiary.Modules.Images.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ImageLocationTests {
    [Theory]
    [InlineData(" relative/legacy-image ", "relative/legacy-image")]
    [InlineData(" https://storage.example/a%2Fb?sig=X%2B%2F ", "https://storage.example/a%2Fb?sig=X%2B%2F")]
    public void PublicLocationInput_TrimsOuterWhitespaceWithoutReinterpretingRelativePathsOrProviderEncoding(string input, string expected) =>
        Assert.Equal(expected, PublicImageUrl.FromInput(input).Value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void PublicLocationInput_RejectsAbsentOrBlankValues(string? input) {
        ArgumentException error = Assert.ThrowsAny<ArgumentException>(() => PublicImageUrl.FromInput(input!));

        Assert.Equal("value", error.ParamName);
    }

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
