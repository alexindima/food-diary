using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ImagesIdConversionTests {
    [Fact]
    public void ImageAssetId_PreservesValueAcrossConversionsAndFormatting() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var id = (ImageAssetId)value;
        Guid roundTrip = id;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal(value, id.Value),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, ImageAssetId.Empty.Value));
    }
}
