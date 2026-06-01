using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Images.Services;

public sealed class ImageAssetAccessService(
    IImageAssetRepository imageAssetRepository,
    IImageStorageService imageStorageService) : IImageAssetAccessService {
    public async Task<Result<ImageAsset?>> ResolveOptionalAsync(
        ImageAssetId? assetId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        if (!assetId.HasValue) {
            return Result.Success<ImageAsset?>(null);
        }

        var asset = await imageAssetRepository.GetByIdAsync(assetId.Value, cancellationToken).ConfigureAwait(false);
        if (asset is null) {
            return Result.Failure<ImageAsset?>(Errors.Image.NotFound(assetId.Value.Value));
        }

        if (asset.UserId != userId) {
            return Result.Failure<ImageAsset?>(Errors.Image.Forbidden());
        }

        var validation = await imageStorageService.ValidateUploadedObjectAsync(asset.ObjectKey, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid) {
            return Result.Failure<ImageAsset?>(Errors.Image.InvalidData(
                validation.Message ?? "Image upload has not completed or is invalid."));
        }

        return Result.Success<ImageAsset?>(asset);
    }
}
