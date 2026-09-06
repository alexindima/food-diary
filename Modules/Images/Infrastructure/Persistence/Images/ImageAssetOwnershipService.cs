using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Images;

internal sealed class ImageAssetOwnershipService(FoodDiaryDbContext context) : IImageAssetOwnershipService {
    public async Task ReassignAsync(IReadOnlyCollection<ImageAssetId> assetIds, UserId targetUserId, CancellationToken cancellationToken) {
        if (assetIds.Count == 0) { return; }
        await context.ImageAssets.Where(asset => assetIds.Contains(asset.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(asset => asset.UserId, targetUserId), cancellationToken).ConfigureAwait(false);
    }
}
