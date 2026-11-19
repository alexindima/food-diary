using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;

namespace FoodDiary.Modules.Images.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class ImageAssetResolutionTests {
    [Fact]
    public void AnUnresolvedAssetPreservesTheRequestedIdentityWithoutInventingAnAsset() {
        var requested = ImageAssetId.New();
        ImageAssetResolution resolution = new ImageAssetResolution.Unresolved(requested);
        Assert.Equal(requested, resolution.ImageAssetId);
        Assert.Null(resolution.ImageAsset);
        ImageAssetResolution empty = default;
        Assert.Null(empty.ImageAssetId);
        Assert.Null(empty.ImageAsset);
    }
}
