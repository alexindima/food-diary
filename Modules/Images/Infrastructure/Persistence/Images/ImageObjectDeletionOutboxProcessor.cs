using FoodDiary.Modules.Images.Domain.ValueObjects;
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
    public async Task<int> ProcessKeysAsync(IReadOnlyList<string> objectKeys, CancellationToken cancellationToken) {
        ensureCleanEntry?.Invoke();
        List<Guid> ids = await messages.AsNoTracking().Where(message => objectKeys.Contains(message.ObjectKey) && message.ProcessedOnUtc == null && message.DeadLetteredOnUtc == null)
            .Select(message => message.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        return await ProcessAsync(ids.Count, ids, cancellationToken).ConfigureAwait(false);
    }

    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) => ProcessAsync(batchSize, allowedMessageIds: null, cancellationToken);

    private Task<int> ProcessAsync(int batchSize, IReadOnlyCollection<Guid>? allowedMessageIds, CancellationToken cancellationToken) =>
        OutboxProcessingEngine.ProcessDueAsync(
            context,
            messages,
            "\"ImageObjectDeletionOutbox\"",
            "image_object_deletion",
            batchSize,
            options.Value,
            timeProvider,
            (message, token) => imageStorageService.DeleteAsync(ObjectStorageKey.FromStoredValue(message.ObjectKey), message.IsConfirmed, token),
            static message => message.ObjectKey,
            logger,
            cancellationToken: cancellationToken, ensureCleanEntry: ensureCleanEntry, allowedMessageIds: allowedMessageIds);
}
