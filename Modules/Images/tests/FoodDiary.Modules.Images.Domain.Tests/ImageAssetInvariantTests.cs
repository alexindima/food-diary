using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
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

    [Fact]
    public void Promote_RequiresConfirmationAndPreservesAssetIdentity() {
        var asset = ImageAsset.Create(UserId.New(), "images/recipe.png", "https://cdn/recipe.png", keepPrivate: true);
        ImageAssetId id = asset.Id;

        Assert.Throws<InvalidOperationException>(asset.Promote);
        Assert.True(asset.IsPrivate);

        asset.Confirm();
        asset.Promote();

        Assert.Multiple(
            () => Assert.False(asset.IsPrivate),
            () => Assert.True(asset.IsConfirmed),
            () => Assert.Equal(id, asset.Id),
            () => Assert.Equal("images/recipe.png", asset.ObjectKey));
    }
}
