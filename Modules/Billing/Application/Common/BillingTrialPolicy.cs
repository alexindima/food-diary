using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Domain.Entities;

namespace FoodDiary.Modules.Billing.Application.Common;

internal static class BillingTrialPolicy {
    internal static bool HasSubscriptionHistory(BillingSubscriptionOverviewReadModel? subscription) =>
        subscription is not null && (subscription.HasPremiumPaymentHistory ||
            (!string.IsNullOrWhiteSpace(subscription.Status) &&
                !string.Equals(subscription.Status, BillingSubscription.PendingCheckoutStatus, StringComparison.OrdinalIgnoreCase)));
}
