using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Products.Infrastructure.Persistence;

internal sealed class ProductsUserDataPurgeParticipant(
    ProductsDbContext context,
    IModuleTransactionCoordinator coordinator,
    IImageAssetOwnershipService imageAssetOwnershipService) : IUserDataPurgeParticipant {
    public int Order => 10;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        if (reassignTarget is { } target) {
            List<ImageAssetId> assetIds = await context.Products
                .Where(item => item.UserId == userId && item.ImageAssetId != null)
                .Select(item => item.ImageAssetId!.Value).ToListAsync(cancellationToken);

            assetIds.AddRange(await context.Products.Where(item => item.UserId == userId)
                .SelectMany(item => item.Images).Select(image => image.ImageAssetId).ToListAsync(cancellationToken));
            await imageAssetOwnershipService.ReassignAsync(assetIds, userId, target, cancellationToken);
            await context.Products.Where(item => item.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserId, target), cancellationToken);
        } else {
            await context.Products.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        }
    }
}
