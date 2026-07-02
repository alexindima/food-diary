using FoodDiary.Application.Abstractions.Images.Common;

namespace FoodDiary.Infrastructure.Persistence.Images;

internal sealed class ImageObjectDeletionOutbox(
    FoodDiaryDbContext context,
    TimeProvider timeProvider) : IImageObjectDeletionOutbox {
    public async Task EnqueueAsync(string objectKey, CancellationToken cancellationToken = default) {
        var message = ImageObjectDeletionOutboxMessage.Create(
            objectKey,
            timeProvider.GetUtcNow().UtcDateTime);

        await context.ImageObjectDeletionOutbox.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
