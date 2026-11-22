using FoodDiary.Modules.Images.Domain.ValueObjects;

namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public static class ImageObjectDeletionOutboxExtensions {
    public static async Task EnqueueAssetObjectsAsync(
        this IImageObjectDeletionOutbox outbox,
        ObjectStorageKey key,
        bool isConfirmed,
        bool isPrivate,
        CancellationToken cancellationToken) {
        await outbox.EnqueueAsync(key, isConfirmed && !isPrivate, cancellationToken);
        if (!isConfirmed || isPrivate) {
            await outbox.EnqueueAsync(key, isConfirmed: true, cancellationToken);
        }

        if (isPrivate) {
            await outbox.EnqueueAsync(key.ForPrivateConfirmedImage(), isConfirmed: false, cancellationToken);
        }
    }
}
