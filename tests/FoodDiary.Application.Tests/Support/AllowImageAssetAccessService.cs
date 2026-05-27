using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests;

public sealed class AllowImageAssetAccessService : IImageAssetAccessService {
    public static AllowImageAssetAccessService Instance { get; } = new();

    public Task<Result<ImageAsset?>> ResolveOptionalAsync(
        ImageAssetId? assetId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        if (!assetId.HasValue) {
            return Task.FromResult(Result.Success<ImageAsset?>(null));
        }

        var asset = ImageAsset.Create(
            userId,
            $"images/{assetId.Value.Value:D}.jpg",
            $"https://cdn.example/{assetId.Value.Value:D}.jpg");
        return Task.FromResult(Result.Success<ImageAsset?>(asset));
    }
}
