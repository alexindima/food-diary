using FoodDiary.Modules.Images.PersistenceModel.Images;
using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Outbox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Images.Infrastructure.Persistence.Images;

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
