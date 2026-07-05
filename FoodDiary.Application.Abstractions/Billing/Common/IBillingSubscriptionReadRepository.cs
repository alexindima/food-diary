using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Billing.Common;

public interface IBillingSubscriptionReadRepository {
    Task<BillingSubscription?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    async Task<BillingSubscriptionOverviewReadModel?> GetOverviewReadModelByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        BillingSubscription? subscription = await GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return subscription is null ? null : ToOverviewReadModel(subscription);
    }

    Task<BillingSubscription?> GetByExternalCustomerIdAsync(string provider, string externalCustomerId, CancellationToken cancellationToken = default);

    Task<BillingSubscription?> GetByExternalSubscriptionIdAsync(string provider, string externalSubscriptionId, CancellationToken cancellationToken = default);

    Task<BillingSubscription?> GetByExternalPaymentMethodIdAsync(
        string provider,
        string externalPaymentMethodId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BillingSubscription>> GetDueForRenewalAsync(
        string provider,
        DateTime dueAtUtc,
        int limit,
        CancellationToken cancellationToken = default);

    private static BillingSubscriptionOverviewReadModel ToOverviewReadModel(BillingSubscription subscription) =>
        new(
            subscription.Id,
            subscription.UserId.Value,
            subscription.Provider,
            subscription.ExternalCustomerId,
            subscription.Plan,
            subscription.Status,
            subscription.CurrentPeriodStartUtc,
            subscription.CurrentPeriodEndUtc,
            subscription.CancelAtPeriodEnd,
            subscription.NextBillingAttemptUtc);
}
