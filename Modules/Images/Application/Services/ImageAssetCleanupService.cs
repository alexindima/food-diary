using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Domain.Entities.Assets;

namespace FoodDiary.Modules.Images.Application.Services;

public sealed class ImageAssetCleanupService(
    IImageAssetWriteRepository imageAssetRepository,
    IImageObjectDeletionOutbox imageObjectDeletionOutbox) : IImageAssetCleanupService {
    public async Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
        if (assetId == ImageAssetId.Empty) {
            return new DeleteImageAssetResult.Refused("invalid");
        }

        ImageAsset? asset = await imageAssetRepository.GetByIdAsync(assetId, cancellationToken);
        if (asset is null) {
            return new DeleteImageAssetResult.Refused("not_found");
        }

        bool inUse = await imageAssetRepository.IsAssetInUseAsync(assetId, cancellationToken);
        if (inUse) {
            return new DeleteImageAssetResult.Refused("in_use");
        }

        await imageObjectDeletionOutbox.EnqueueAssetObjectsAsync(
            ObjectStorageKey.FromStoredValue(asset.ObjectKey), asset.IsConfirmed, asset.IsPrivate, cancellationToken);
        await imageAssetRepository.DeleteAsync(asset, cancellationToken);
        return DeleteImageAssetResult.Success;
    }
}
