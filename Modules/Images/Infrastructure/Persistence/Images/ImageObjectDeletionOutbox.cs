using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Modules.Images.PersistenceModel.Images;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Images.Application.Abstractions.Common;

namespace FoodDiary.Modules.Images.Infrastructure.Persistence.Images;

internal sealed class ImageObjectDeletionOutbox(
    DbSet<ImageObjectDeletionOutboxMessage> messages,
    TimeProvider timeProvider) : IImageObjectDeletionOutbox {
    public async Task EnqueueAsync(ObjectStorageKey key, bool isConfirmed, CancellationToken cancellationToken = default) {
        var message = ImageObjectDeletionOutboxMessage.Create(
            key.Value,
            isConfirmed,
            timeProvider.GetUtcNow().UtcDateTime);

        await messages.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public Task EnqueueAsync(ObjectStorageKey key, CancellationToken cancellationToken = default) =>
        EnqueueAsync(key, isConfirmed: true, cancellationToken);
}
