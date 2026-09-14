using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Billing.Infrastructure.Persistence;

public sealed class BillingSubscriptionRepository(DbSet<BillingSubscription> subscriptions, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IBillingSubscriptionReadModelRepository, IBillingSubscriptionWriteRepository {
    public async Task<BillingSubscription?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await subscriptions
            .FirstOrDefaultAsync(subscription => subscription.UserId == userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BillingSubscriptionOverviewReadModel?> GetOverviewReadModelByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await subscriptions
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
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<BillingSubscription?> GetByExternalCustomerIdAsync(
        string provider,
        string externalCustomerId,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await subscriptions
            .FirstOrDefaultAsync(
                subscription => subscription.Provider == provider && subscription.ExternalCustomerId == externalCustomerId,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<BillingSubscription?> GetByExternalSubscriptionIdAsync(
        string provider,
        string externalSubscriptionId,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await subscriptions
            .FirstOrDefaultAsync(
                subscription => subscription.Provider == provider && subscription.ExternalSubscriptionId == externalSubscriptionId,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<BillingSubscription?> GetByExternalPaymentMethodIdAsync(
        string provider,
        string externalPaymentMethodId,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await subscriptions
            .FirstOrDefaultAsync(
                subscription => subscription.Provider == provider && subscription.ExternalPaymentMethodId == externalPaymentMethodId,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BillingSubscription>> GetDueForRenewalAsync(
        string provider,
        DateTime dueAtUtc,
        int limit,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
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
        // Access synchronization can update a subscription created by this same webhook before its first save.
        if (subscriptions.Entry(subscription).State != EntityState.Added) {
            subscriptions.Update(subscription);
        }
        return Task.CompletedTask;
    }

    private Task SynchronizeTransactionAsync(CancellationToken cancellationToken) =>
        synchronizeTransactionAsync?.Invoke(cancellationToken) ?? Task.CompletedTask;
}
