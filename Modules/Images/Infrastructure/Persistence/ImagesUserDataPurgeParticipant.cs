using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Images.Infrastructure.Persistence;

internal sealed class ImagesUserDataPurgeParticipant(
    ImagesDbContext context,
    IModuleTransactionCoordinator coordinator,
    IImageObjectDeletionOutbox imageObjectDeletionOutbox) : IUserDataPurgeParticipant, ITelegramDiaryContentTransfer {
    public int Order => 135;

    public async Task TransferAsync(UserId sourceId, UserId targetId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        await context.ImageAssets.Where(item => item.UserId == sourceId && item.IsConfirmed)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserId, targetId), cancellationToken);
    }

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        var deletedImages = await context.ImageAssets
            .Where(asset => asset.UserId == userId)
            .Select(asset => new { asset.ObjectKey, asset.IsConfirmed })
            .ToListAsync(cancellationToken);

        foreach (var image in deletedImages.DistinctBy(static image => new { image.ObjectKey, image.IsConfirmed })) {
            await imageObjectDeletionOutbox.EnqueueAsync(ObjectStorageKey.FromStoredValue(image.ObjectKey), image.IsConfirmed, cancellationToken);
            if (!image.IsConfirmed) {
                await imageObjectDeletionOutbox.EnqueueAsync(ObjectStorageKey.FromStoredValue(image.ObjectKey), isConfirmed: true, cancellationToken);
            }
        }

        await context.ImageAssets
            .Where(asset => asset.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
