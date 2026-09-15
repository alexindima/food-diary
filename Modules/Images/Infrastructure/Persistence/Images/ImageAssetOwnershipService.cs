using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Images.Infrastructure.Persistence.Images;

internal sealed class ImageAssetOwnershipService(ImagesDbContext context) : IImageAssetOwnershipService {
    public async Task ReassignAsync(IReadOnlyCollection<ImageAssetId> assetIds, UserId targetUserId, CancellationToken cancellationToken) {
        if (assetIds.Count == 0) { return; }
        await context.ImageAssets.Where(asset => assetIds.Contains(asset.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(asset => asset.UserId, targetUserId), cancellationToken).ConfigureAwait(false);
    }
}
