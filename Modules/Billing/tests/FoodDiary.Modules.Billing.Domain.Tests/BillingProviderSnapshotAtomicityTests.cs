using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingProviderSnapshotAtomicityTests {
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    private static BillingPayment CreateBillingPayment() {
        return BillingPayment.Create(
            UserId.New(),
            billingSubscriptionId: null,
            BillingProviderNames.Stripe,
            "payment_1",
            "customer_1",
            externalSubscriptionId: null,
            externalPaymentMethodId: null,
            externalPriceId: null,
            plan: null,
            "active",
            BillingPaymentKinds.Webhook,
            amount: null,
            currency: null,
            currentPeriodStartUtc: null,
            currentPeriodEndUtc: null,
            webhookEventId: null,
            providerMetadataJson: null);
    }

    [Fact]
    public void BillingSubscription_InvalidSnapshotDoesNotPartiallyMutateState() {
        var subscription = BillingSubscription.CreatePending(
            UserId.New(),
            BillingProviderNames.Stripe,
            "customer_1",
            "price_1",
            "monthly");

        Assert.Throws<ArgumentException>(() => subscription.ApplyProviderSnapshot(
            BillingProviderNames.Paddle,
            "sub_changed",
            "pm_changed",
            "price_changed",
            "annual",
            "active",
            Now.AddDays(2),
            Now.AddDays(1),
            cancelAtPeriodEnd: false,
            canceledAtUtc: null,
            trialStartUtc: null,
            trialEndUtc: null,
            "evt_1",
            Now));

        Assert.Multiple(
            () => Assert.Equal(BillingProviderNames.Stripe, subscription.Provider),
            () => Assert.Null(subscription.ExternalSubscriptionId),
            () => Assert.Equal("price_1", subscription.ExternalPriceId),
            () => Assert.Equal(BillingSubscription.PendingCheckoutStatus, subscription.Status));
    }

    [Fact]
    public void BillingPayment_InvalidProviderResultDoesNotPartiallyMutateState() {
        BillingPayment payment = CreateBillingPayment();

        Assert.Throws<ArgumentException>(() => payment.ApplyProviderResult(
            billingSubscriptionId: Guid.NewGuid(),
            externalCustomerId: "changed",
            externalSubscriptionId: null,
            externalPaymentMethodId: null,
            externalPriceId: null,
            plan: null,
            status: "active",
            kind: BillingPaymentKinds.Webhook,
            amount: null,
            currency: null,
            currentPeriodStartUtc: Now.AddDays(2),
            currentPeriodEndUtc: Now.AddDays(1),
            webhookEventId: null,
            providerMetadataJson: null));

        Assert.Multiple(
            () => Assert.Null(payment.BillingSubscriptionId),
            () => Assert.Equal("customer_1", payment.ExternalCustomerId),
            () => Assert.Null(payment.CurrentPeriodStartUtc));
    }
}
