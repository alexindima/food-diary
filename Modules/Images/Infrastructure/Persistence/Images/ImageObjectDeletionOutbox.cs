using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Abstractions.Images.Common;

namespace FoodDiary.Infrastructure.Persistence.Images;

internal sealed class ImageObjectDeletionOutbox(
    DbSet<ImageObjectDeletionOutboxMessage> messages,
    TimeProvider timeProvider) : IImageObjectDeletionOutbox {
    public async Task EnqueueAsync(string objectKey, bool isConfirmed, CancellationToken cancellationToken = default) {
        var message = ImageObjectDeletionOutboxMessage.Create(
            objectKey,
            isConfirmed,
            timeProvider.GetUtcNow().UtcDateTime);

        await messages.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public Task EnqueueAsync(string objectKey, CancellationToken cancellationToken = default) =>
        EnqueueAsync(objectKey, isConfirmed: true, cancellationToken);
}
