using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Images.Infrastructure.Persistence;

internal sealed class ImagesUserDataPurgeParticipant(
    FoodDiaryDbContext context,
    IImageObjectDeletionOutbox imageObjectDeletionOutbox) : IUserDataPurgeParticipant {
    public int Order => 110;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
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
