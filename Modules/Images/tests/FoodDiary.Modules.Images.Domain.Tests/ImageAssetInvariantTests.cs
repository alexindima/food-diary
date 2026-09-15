using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ImageAssetInvariantTests {
    [Fact]
    public void Create_TrimsValues() {
        var asset = ImageAsset.Create(UserId.New(), "  images/a.png  ", "  https://cdn/a.png  ");

        Assert.Equal("images/a.png", asset.ObjectKey);
        Assert.Equal("https://cdn/a.png", asset.Url);
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ImageAsset.Create(UserId.Empty, "images/a.png", "https://cdn/a.png"));
    }

    [Fact]
    public void Create_WithBlankObjectKey_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ImageAsset.Create(UserId.New(), "   ", "https://cdn/a.png"));
    }

    [Fact]
    public void Create_WithBlankUrl_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ImageAsset.Create(UserId.New(), "images/a.png", "   "));
    }
}
