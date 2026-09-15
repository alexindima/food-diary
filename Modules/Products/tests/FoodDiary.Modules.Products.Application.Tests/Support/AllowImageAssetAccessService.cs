using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Tests.Support;

[ExcludeFromCodeCoverage]
public sealed class AllowImageAssetAccessService : IImageAssetAccessService {
    public static AllowImageAssetAccessService Instance { get; } = new();

    public Task<Result<ImageAssetReadModel?>> ResolveOptionalAsync(
        ImageAssetId? assetId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        if (!assetId.HasValue) {
            return Task.FromResult(Result.Success<ImageAssetReadModel?>(value: null));
        }

        var asset = ImageAsset.Create(
            userId,
            $"images/{assetId.Value.Value:D}.jpg",
            $"https://cdn.example/{assetId.Value.Value:D}.jpg");
        return Task.FromResult(Result.Success<ImageAssetReadModel?>(new ImageAssetReadModel(asset.Id, asset.Url)));
    }
}
