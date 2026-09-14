using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Billing.Infrastructure.Persistence;

public sealed class BillingWebhookEventRepository(DbSet<BillingWebhookEvent> webhookEvents, TimeProvider? timeProvider = null, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IBillingWebhookEventReadRepository, IBillingWebhookEventWriteRepository {
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    public async Task<bool> ExistsAsync(
        string provider,
        string eventId,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await webhookEvents
            .AnyAsync(
                webhookEvent => webhookEvent.Provider == provider && webhookEvent.EventId == eventId,
                cancellationToken).ConfigureAwait(false);
    }

    public Task<BillingWebhookEvent> AddAsync(
        BillingWebhookEvent webhookEvent,
        CancellationToken cancellationToken = default) {
        webhookEvents.Add(webhookEvent);
        return Task.FromResult(webhookEvent);
    }

    public async Task<BillingWebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await webhookEvents.SingleOrDefaultAsync(webhookEvent => webhookEvent.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BillingWebhookEvent>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await webhookEvents
            .Where(webhookEvent =>
                (webhookEvent.Status == BillingWebhookEvent.ReceivedStatus || webhookEvent.Status == BillingWebhookEvent.FailedStatus) &&
                webhookEvent.AttemptCount < 10 &&
                (webhookEvent.NextAttemptAtUtc == null || webhookEvent.NextAttemptAtUtc <= _timeProvider.GetUtcNow().UtcDateTime))
            .OrderBy(webhookEvent => webhookEvent.ReceivedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task UpdateAsync(BillingWebhookEvent webhookEvent, CancellationToken cancellationToken = default) {
        webhookEvents.Update(webhookEvent);
        return Task.CompletedTask;
    }

    private Task SynchronizeTransactionAsync(CancellationToken cancellationToken) =>
        synchronizeTransactionAsync?.Invoke(cancellationToken) ?? Task.CompletedTask;
}
