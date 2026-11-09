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
    IImageAssetOwnershipService imageAssetOwnershipService) : IUserDataPurgeParticipant, ITelegramDiaryContentTransfer {
    public int Order => 10;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        if (reassignTarget is { } target) {
            await TransferAsync(userId, target, cancellationToken);
            return;
        }

        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        await context.Products.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task TransferAsync(UserId sourceId, UserId targetId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        List<ImageAssetId> assetIds = await context.Products
            .Where(item => item.UserId == sourceId && item.ImageAssetId != null)
            .Select(item => item.ImageAssetId!.Value).ToListAsync(cancellationToken);

        assetIds.AddRange(await context.Products.Where(item => item.UserId == sourceId)
            .SelectMany(item => item.Images).Select(image => image.ImageAssetId).ToListAsync(cancellationToken));
        await imageAssetOwnershipService.ReassignAsync(assetIds, sourceId, targetId, cancellationToken);
        await context.Products.Where(item => item.UserId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserId, targetId), cancellationToken);
    }
}
