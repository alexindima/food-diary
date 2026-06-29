using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Infrastructure.Persistence.Billing;

public sealed class BillingWebhookEventRepository(FoodDiaryDbContext context) : IBillingWebhookEventRepository {
    public Task<bool> ExistsAsync(
        string provider,
        string eventId,
        CancellationToken cancellationToken = default) {
        return context.BillingWebhookEvents
            .AnyAsync(
                webhookEvent => webhookEvent.Provider == provider && webhookEvent.EventId == eventId,
                cancellationToken);
    }

    public async Task<BillingWebhookEvent> AddAsync(
        BillingWebhookEvent webhookEvent,
        CancellationToken cancellationToken = default) {
        context.BillingWebhookEvents.Add(webhookEvent);
        try {
            // Follow-up: move webhook idempotency translation to an explicit transaction boundary.
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        } catch (DbUpdateException ex) when (IsDuplicateWebhookEvent(ex)) {
            context.Entry(webhookEvent).State = EntityState.Detached;
            throw new BillingWebhookEventAlreadyProcessedException(webhookEvent.Provider, webhookEvent.EventId);
        }

        return webhookEvent;
    }

    private static bool IsDuplicateWebhookEvent(DbUpdateException exception) =>
        exception.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_BillingWebhookEvents_Provider_EventId",
        };
}
