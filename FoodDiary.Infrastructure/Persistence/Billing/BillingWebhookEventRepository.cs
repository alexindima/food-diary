using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Billing;

public sealed class BillingWebhookEventRepository(FoodDiaryDbContext context, TimeProvider? timeProvider = null) : IBillingWebhookEventRepository {
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    public Task<bool> ExistsAsync(
        string provider,
        string eventId,
        CancellationToken cancellationToken = default) {
        return context.BillingWebhookEvents
            .AnyAsync(
                webhookEvent => webhookEvent.Provider == provider && webhookEvent.EventId == eventId,
                cancellationToken);
    }

    public Task<BillingWebhookEvent> AddAsync(
        BillingWebhookEvent webhookEvent,
        CancellationToken cancellationToken = default) {
        context.BillingWebhookEvents.Add(webhookEvent);
        return Task.FromResult(webhookEvent);
    }

    public Task<BillingWebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.BillingWebhookEvents.SingleOrDefaultAsync(webhookEvent => webhookEvent.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BillingWebhookEvent>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken = default) {
        return await context.BillingWebhookEvents
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
        context.BillingWebhookEvents.Update(webhookEvent);
        return Task.CompletedTask;
    }
}
