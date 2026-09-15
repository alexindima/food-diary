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
            return new DeleteImageAssetResult(Deleted: false, "invalid");
        }

        ImageAsset? asset = await imageAssetRepository.GetByIdAsync(assetId, cancellationToken).ConfigureAwait(false);
        if (asset is null) {
            return new DeleteImageAssetResult(Deleted: false, "not_found");
        }

        bool inUse = await imageAssetRepository.IsAssetInUseAsync(assetId, cancellationToken).ConfigureAwait(false);
        if (inUse) {
            return new DeleteImageAssetResult(Deleted: false, "in_use");
        }

        await EnqueueObjectDeletionAsync(asset, cancellationToken).ConfigureAwait(false);
        await imageAssetRepository.DeleteAsync(asset, cancellationToken).ConfigureAwait(false);
        return new DeleteImageAssetResult(Deleted: true);
    }

    private async Task EnqueueObjectDeletionAsync(ImageAsset asset, CancellationToken cancellationToken) {
        await imageObjectDeletionOutbox.EnqueueAsync(asset.ObjectKey, asset.IsConfirmed, cancellationToken).ConfigureAwait(false);
        if (!asset.IsConfirmed) {
            await imageObjectDeletionOutbox.EnqueueAsync(asset.ObjectKey, isConfirmed: true, cancellationToken).ConfigureAwait(false);
        }
    }
}
