using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;

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
        await context.SaveChangesAsync(cancellationToken);
        return webhookEvent;
    }
}
