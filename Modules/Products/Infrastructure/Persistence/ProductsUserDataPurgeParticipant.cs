using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

internal sealed class ProductsUserDataPurgeParticipant(
    FoodDiaryDbContext context,
    IImageAssetOwnershipService imageAssetOwnershipService) : IUserDataPurgeParticipant {
    public int Order => 10;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        if (reassignTarget is { } target) {
            List<ImageAssetId> assetIds = await context.Products
                .Where(item => item.UserId == userId && item.ImageAssetId != null)
                .Select(item => item.ImageAssetId!.Value).ToListAsync(cancellationToken).ConfigureAwait(false);

            await imageAssetOwnershipService.ReassignAsync(assetIds, target, cancellationToken).ConfigureAwait(false);
            await context.Products.Where(item => item.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserId, target), cancellationToken).ConfigureAwait(false);
        } else {
            await context.Products.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
