using FoodDiary.Modules.Notifications.PersistenceModel;
using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Outbox.Infrastructure.Persistence;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Notifications.Infrastructure.Persistence;

internal sealed class NotificationWebPushOutboxProcessor(
    DbContext context,
    DbSet<NotificationWebPushOutboxMessage> messages,
    IWebPushNotificationSender webPushNotificationSender,
    IOptions<OutboxProcessingOptions> options,
    TimeProvider timeProvider,
    ILogger<NotificationWebPushOutboxProcessor> logger, Action? ensureCleanEntry = null) : INotificationWebPushOutboxProcessor {
    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
        OutboxProcessingEngine.ProcessDueAsync(
            context,
            messages,
            "\"NotificationWebPushOutbox\"",
            "notification_web_push",
            batchSize,
            options.Value,
            timeProvider,
            async (message, token) => {
                message.RecordDeliveryAttempt(timeProvider.GetUtcNow().UtcDateTime);
                WebPushDeliveryOutcome outcome = await webPushNotificationSender.SendBatchAsync(message.Notification,
                    message.GetCompletedSubscriptionIds(), token);
                message.RecordCompletedSubscriptions(outcome.CompletedSubscriptionIds);
                if (outcome.RequiresRetry) {
                    throw new HttpRequestException("Web push delivery has unfinished subscriptions.");
                }
            },
            static message => message.NotificationId.Value,
            logger,
            messages.Include(message => message.Notification),
            cancellationToken, ensureCleanEntry: ensureCleanEntry,
            selectMessageIdsAsync: SelectNextMessageIdsAsync);

    private async Task<IReadOnlyCollection<Guid>> SelectNextMessageIdsAsync(DateTime nowUtc, CancellationToken cancellationToken) {
        // Keep retry scheduling separate from actual service history. Recompute after
        // every bounded delivery so an old backlog cannot monopolize later claims.
        var history = messages.AsNoTracking()
            .GroupBy(message => message.Notification.UserId)
            .Select(group => new {
                UserId = group.Key,
                LastAttemptOnUtc = group.Max(message => message.LastAttemptOnUtc ?? message.ProcessedOnUtc ?? message.DeadLetteredOnUtc),
            });
        Guid? nextId = await (
            from message in messages.AsNoTracking()
            join userHistory in history on message.Notification.UserId equals userHistory.UserId
            where message.ProcessedOnUtc == null && message.DeadLetteredOnUtc == null &&
                  message.NextAttemptOnUtc <= nowUtc &&
                  (message.LockedUntilUtc == null || message.LockedUntilUtc <= nowUtc)
            orderby userHistory.LastAttemptOnUtc.HasValue, userHistory.LastAttemptOnUtc, message.CreatedOnUtc, message.Id
            select (Guid?)message.Id).FirstOrDefaultAsync(cancellationToken);
        return nextId.HasValue ? [nextId.Value] : [];
    }
}
