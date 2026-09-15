using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Images.Infrastructure.Persistence;

internal sealed class ImagesUserDataPurgeParticipant(
    ImagesDbContext context,
    IModuleTransactionCoordinator coordinator,
    IImageObjectDeletionOutbox imageObjectDeletionOutbox) : IUserDataPurgeParticipant {
    public int Order => 135;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
        var deletedImages = await context.ImageAssets
            .Where(asset => asset.UserId == userId)
            .Select(asset => new { asset.ObjectKey, asset.IsConfirmed })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var image in deletedImages.DistinctBy(static image => new { image.ObjectKey, image.IsConfirmed })) {
            await imageObjectDeletionOutbox.EnqueueAsync(image.ObjectKey, image.IsConfirmed, cancellationToken).ConfigureAwait(false);
            if (!image.IsConfirmed) {
                await imageObjectDeletionOutbox.EnqueueAsync(image.ObjectKey, isConfirmed: true, cancellationToken).ConfigureAwait(false);
            }
        }

        await context.ImageAssets
            .Where(asset => asset.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
