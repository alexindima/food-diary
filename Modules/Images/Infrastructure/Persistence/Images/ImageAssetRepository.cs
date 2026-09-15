using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Images.Infrastructure.Persistence.Images;

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
}
