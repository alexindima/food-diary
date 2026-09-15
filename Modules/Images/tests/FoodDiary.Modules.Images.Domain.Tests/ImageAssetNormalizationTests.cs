using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ImageAssetNormalizationTests {
    [Fact]
    public void MiscDomainMethods_CoverRemainingBranches() {
        var asset = ImageAsset.Create(UserId.New(), " object/key ", " https://img ");
        Assert.Multiple(
            () => Assert.Equal("object/key", asset.ObjectKey),
            () => Assert.Equal("https://img", asset.Url));
    }
}
