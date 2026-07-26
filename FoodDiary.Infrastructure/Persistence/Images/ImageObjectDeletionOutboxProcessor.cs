using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Images;

internal sealed class ImageObjectDeletionOutboxProcessor(
    FoodDiaryDbContext context,
    IImageStorageService imageStorageService,
    TimeProvider timeProvider,
    ILogger<ImageObjectDeletionOutboxProcessor> logger) : IImageObjectDeletionOutboxProcessor {
    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
        OutboxProcessingEngine.ProcessDueAsync(
            context,
            context.ImageObjectDeletionOutbox,
            "\"ImageObjectDeletionOutbox\"",
            "image_object_deletion",
            batchSize,
            timeProvider,
            (message, token) => imageStorageService.DeleteAsync(message.ObjectKey, token),
            static message => message.ObjectKey,
            logger,
            cancellationToken: cancellationToken);
}
