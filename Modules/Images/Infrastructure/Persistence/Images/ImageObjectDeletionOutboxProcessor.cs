using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Persistence.Images;

internal sealed class ImageObjectDeletionOutboxProcessor(
    DbContext context,
    DbSet<ImageObjectDeletionOutboxMessage> messages,
    IImageStorageService imageStorageService,
    IOptions<OutboxProcessingOptions> options,
    TimeProvider timeProvider,
    ILogger<ImageObjectDeletionOutboxProcessor> logger, Action? ensureCleanEntry = null) : IImageObjectDeletionOutboxProcessor {
    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
        OutboxProcessingEngine.ProcessDueAsync(
            context,
            messages,
            "\"ImageObjectDeletionOutbox\"",
            "image_object_deletion",
            batchSize,
            options.Value,
            timeProvider,
            (message, token) => imageStorageService.DeleteAsync(message.ObjectKey, message.IsConfirmed, token),
            static message => message.ObjectKey,
            logger,
            cancellationToken: cancellationToken, ensureCleanEntry: ensureCleanEntry);
}
