using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Images.Services;

public sealed class ImageAssetCleanupService(
    IImageAssetWriteRepository imageAssetRepository,
    IImageObjectDeletionOutbox imageObjectDeletionOutbox,
    IImageStorageService imageStorageService,
    ILogger<ImageAssetCleanupService> logger,
    IUnitOfWork unitOfWork) : IImageAssetCleanupService {
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

        await imageAssetRepository.DeleteAsync(asset, cancellationToken).ConfigureAwait(false);
        await imageObjectDeletionOutbox.EnqueueAsync(asset.ObjectKey, cancellationToken).ConfigureAwait(false);
        return new DeleteImageAssetResult(Deleted: true);
    }

    public async Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) {
        if (batchSize <= 0) {
            return 0;
        }

        DateTime normalizedOlderThanUtc = olderThanUtc.Kind switch {
            DateTimeKind.Utc => olderThanUtc,
            _ => olderThanUtc.ToUniversalTime(),
        };

        IReadOnlyList<ImageAsset> candidates = await imageAssetRepository.GetUnusedOlderThanAsync(normalizedOlderThanUtc, batchSize, cancellationToken).ConfigureAwait(false);
        int removed = 0;

        foreach (ImageAsset asset in candidates) {
            try {
                await imageStorageService.DeleteAsync(asset.ObjectKey, cancellationToken).ConfigureAwait(false);
                await imageAssetRepository.DeleteAsync(asset, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                removed++;
            } catch (Exception ex) {
                logger.LogWarning(ex, "Failed to remove orphan image asset {AssetId}", asset.Id);
            }
        }

        return removed;
    }
}
