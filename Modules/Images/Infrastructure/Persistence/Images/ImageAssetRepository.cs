using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Images;

public sealed class ImageAssetRepository(DbSet<ImageAsset> assets, IImageAssetUsageQuery usageQuery) : IImageAssetRepository {
    public Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default) {
        assets.Add(asset);
        return Task.FromResult(asset);
    }

    public async Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default) {
        return await assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImageAsset?> GetOwnedByIdAsync(ImageAssetId id, UserId userId, CancellationToken cancellationToken = default) {
        return await assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ImageAsset?> GetOwnedForUpdateAsync(ImageAssetId id, UserId userId, CancellationToken cancellationToken = default) {
        return await assets
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default) {
        assets.Attach(asset);
        assets.Remove(asset);
        return Task.CompletedTask;
    }

    public Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
        usageQuery.IsAssetInUseAsync(assetId, cancellationToken);

    public async Task<IReadOnlyList<ImageAsset>> GetUnusedOlderThanAsync(
        DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) {
        IReadOnlyList<ImageAssetId> candidates = await usageQuery.GetUnusedIdsOlderThanAsync(olderThanUtc, batchSize, cancellationToken).ConfigureAwait(false);
        return await assets.AsNoTracking().Where(asset => candidates.Contains(asset.Id))
            .OrderBy(asset => asset.CreatedOnUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
