using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Entities;

namespace FoodDiary.Modules.Products.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProductImageTests {
    [Fact]
    public void Constructor_RejectsEmptyImageId() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ProductImage(default, "https://example.test/image.jpg", 0));
        Assert.Equal("imageAssetId", error.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_RejectsMissingUrl(string? url) {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new ProductImage(ImageAssetId.New(), url!, 0));
        Assert.Equal("imageUrl", error.ParamName);
    }

    [Fact]
    public void Constructor_EnforcesMaximumUrlLength() {
        var id = ImageAssetId.New();
        string url = new('a', Product.ImageUrlMaxLength);
        var image = new ProductImage(id, url, 3);
        Assert.Multiple(() => Assert.Equal(id, image.ImageAssetId), () => Assert.Equal(url, image.ImageUrl), () => Assert.Equal(3, image.Position));
        Assert.Throws<ArgumentException>(() => new ProductImage(id, url + "a", 3));
    }
}
