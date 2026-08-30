using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Images.Services;

public sealed class ImageAssetAccessService(
    IImageAssetReadRepository imageAssetRepository) : IImageAssetAccessService {
    public async Task<Result<ImageAsset?>> ResolveOptionalAsync(
        ImageAssetId? assetId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        if (!assetId.HasValue) {
            return Result.Success<ImageAsset?>(value: null);
        }

        ImageAsset? asset = await imageAssetRepository.GetOwnedByIdAsync(assetId.Value, userId, cancellationToken).ConfigureAwait(false);
        if (asset is null) {
            return Result.Failure<ImageAsset?>(Errors.Image.NotFound(assetId.Value.Value));
        }

        if (!asset.IsConfirmed) {
            return Result.Failure<ImageAsset?>(Errors.Image.InvalidData(
                "Image upload has not been confirmed."));
        }

        return Result.Success<ImageAsset?>(asset);
    }
}
