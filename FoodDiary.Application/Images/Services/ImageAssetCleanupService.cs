using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Application.Images.Services;

public sealed class ImageAssetCleanupService(
    IImageAssetRepository imageAssetRepository,
    IImageStorageService imageStorageService,
    ILogger<ImageAssetCleanupService> logger) : IImageAssetCleanupService
{
    public async Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default)
    {
        var asset = await imageAssetRepository.GetByIdAsync(assetId, cancellationToken);
        if (asset is null)
        {
            return new DeleteImageAssetResult(false, "not_found");
        }

        var inUse = await imageAssetRepository.IsAssetInUse(assetId, cancellationToken);
        if (inUse)
        {
            return new DeleteImageAssetResult(false, "in_use");
        }

        try
        {
            await imageStorageService.DeleteAsync(asset.ObjectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete image object {ObjectKey}", asset.ObjectKey);
            return new DeleteImageAssetResult(false, "storage_error");
        }

        await imageAssetRepository.DeleteAsync(asset, cancellationToken);
        return new DeleteImageAssetResult(true);
    }

    public async Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        var candidates = await imageAssetRepository.GetUnusedOlderThanAsync(olderThanUtc, batchSize, cancellationToken);
        var removed = 0;

        foreach (var asset in candidates)
        {
            try
            {
                await imageStorageService.DeleteAsync(asset.ObjectKey, cancellationToken);
                await imageAssetRepository.DeleteAsync(asset, cancellationToken);
                removed++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to remove orphan image asset {AssetId}", asset.Id);
            }
        }

        return removed;
    }
}

