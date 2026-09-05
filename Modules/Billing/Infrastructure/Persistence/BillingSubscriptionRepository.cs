using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Billing.Infrastructure.Persistence;

public sealed class BillingSubscriptionRepository(DbSet<BillingSubscription> subscriptions) : IBillingSubscriptionRepository {
    public Task<BillingSubscription?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) {
        return subscriptions
            .FirstOrDefaultAsync(subscription => subscription.UserId == userId, cancellationToken);
    }

    public Task<BillingSubscriptionOverviewReadModel?> GetOverviewReadModelByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.UserId == userId)
            .Select(subscription => new BillingSubscriptionOverviewReadModel(
                subscription.Id,
                subscription.UserId.Value,
                subscription.Provider,
                subscription.ExternalCustomerId,
                subscription.Plan,
                subscription.Status,
                subscription.CurrentPeriodStartUtc,
                subscription.CurrentPeriodEndUtc,
                subscription.CancelAtPeriodEnd,
                subscription.NextBillingAttemptUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<BillingSubscription?> GetByExternalCustomerIdAsync(
        string provider,
        string externalCustomerId,
        CancellationToken cancellationToken = default) {
        return subscriptions
            .FirstOrDefaultAsync(
                subscription => subscription.Provider == provider && subscription.ExternalCustomerId == externalCustomerId,
                cancellationToken);
    }

    public Task<BillingSubscription?> GetByExternalSubscriptionIdAsync(
        string provider,
        string externalSubscriptionId,
        CancellationToken cancellationToken = default) {
        return subscriptions
            .FirstOrDefaultAsync(
                subscription => subscription.Provider == provider && subscription.ExternalSubscriptionId == externalSubscriptionId,
                cancellationToken);
    }

    public Task<BillingSubscription?> GetByExternalPaymentMethodIdAsync(
        string provider,
        string externalPaymentMethodId,
        CancellationToken cancellationToken = default) {
        return subscriptions
            .FirstOrDefaultAsync(
                subscription => subscription.Provider == provider && subscription.ExternalPaymentMethodId == externalPaymentMethodId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<BillingSubscription>> GetDueForRenewalAsync(
        string provider,
        DateTime dueAtUtc,
        int limit,
        CancellationToken cancellationToken = default) {
        return await subscriptions
            .Where(subscription =>
                subscription.Provider == provider &&
                !subscription.CancelAtPeriodEnd &&
                subscription.ExternalPaymentMethodId != null &&
                subscription.NextBillingAttemptUtc != null &&
                subscription.NextBillingAttemptUtc <= dueAtUtc &&
                (subscription.Status == "active" ||
                    subscription.Status == "trialing" ||
                    subscription.Status == "past_due"))
            .OrderBy(subscription => subscription.NextBillingAttemptUtc)
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<BillingSubscription> AddAsync(
        BillingSubscription subscription,
        CancellationToken cancellationToken = default) {
        subscriptions.Add(subscription);
        return Task.FromResult(subscription);
    }

    public Task UpdateAsync(BillingSubscription subscription, CancellationToken cancellationToken = default) {
        subscriptions.Update(subscription);
        return Task.CompletedTask;
    }
}
