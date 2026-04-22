using FoodDiary.Application.Billing.Common;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Billing;

public sealed class BillingSubscriptionRepository(FoodDiaryDbContext context) : IBillingSubscriptionRepository {
    public Task<BillingSubscription?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) {
        return context.BillingSubscriptions
            .FirstOrDefaultAsync(subscription => subscription.UserId == userId, cancellationToken);
    }

    public Task<BillingSubscription?> GetByExternalCustomerIdAsync(
        string provider,
        string externalCustomerId,
        CancellationToken cancellationToken = default) {
        return context.BillingSubscriptions
            .FirstOrDefaultAsync(
                subscription => subscription.Provider == provider && subscription.ExternalCustomerId == externalCustomerId,
                cancellationToken);
    }

    public Task<BillingSubscription?> GetByExternalSubscriptionIdAsync(
        string provider,
        string externalSubscriptionId,
        CancellationToken cancellationToken = default) {
        return context.BillingSubscriptions
            .FirstOrDefaultAsync(
                subscription => subscription.Provider == provider && subscription.ExternalSubscriptionId == externalSubscriptionId,
                cancellationToken);
    }

    public async Task<BillingSubscription> AddAsync(
        BillingSubscription subscription,
        CancellationToken cancellationToken = default) {
        context.BillingSubscriptions.Add(subscription);
        await context.SaveChangesAsync(cancellationToken);
        return subscription;
    }

    public async Task UpdateAsync(BillingSubscription subscription, CancellationToken cancellationToken = default) {
        context.BillingSubscriptions.Update(subscription);
        await context.SaveChangesAsync(cancellationToken);
    }
}
