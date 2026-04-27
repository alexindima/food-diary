using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public sealed class BillingInvariantTests {
    private static readonly UserId UserId = UserId.New();
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BillingSubscription_ActiveSnapshot_SchedulesNextBillingAttemptAtPeriodEnd() {
        var subscription = BillingSubscription.CreatePending(
            UserId,
            BillingProviderNames.YooKassa,
            "customer_1",
            "price_monthly",
            "monthly");

        subscription.ApplyProviderSnapshot(
            BillingProviderNames.YooKassa,
            "payment_1",
            "pm_1",
            "price_monthly",
            "monthly",
            "active",
            Now,
            Now.AddMonths(1),
            false,
            null,
            null,
            null,
            "evt_1",
            Now,
            "{\"provider\":\"yookassa\"}");

        Assert.Equal(Now.AddMonths(1), subscription.NextBillingAttemptUtc);
        Assert.Equal("pm_1", subscription.ExternalPaymentMethodId);
        Assert.Equal("{\"provider\":\"yookassa\"}", subscription.ProviderMetadataJson);
    }

    [Fact]
    public void BillingSubscription_CancelAtPeriodEnd_DoesNotScheduleNextBillingAttempt() {
        var subscription = BillingSubscription.CreatePending(
            UserId,
            BillingProviderNames.Paddle,
            "customer_1",
            "price_monthly",
            "monthly");

        subscription.ApplyProviderSnapshot(
            BillingProviderNames.Paddle,
            "sub_1",
            "pm_1",
            "price_monthly",
            "monthly",
            "active",
            Now,
            Now.AddMonths(1),
            true,
            null,
            null,
            null,
            "evt_1",
            Now);

        Assert.Null(subscription.NextBillingAttemptUtc);
    }

    [Fact]
    public void BillingPayment_Create_PreservesProviderIdentifiersAndAuditFields() {
        var payment = BillingPayment.Create(
            UserId,
            Guid.NewGuid(),
            " yookassa ",
            " payment_1 ",
            " customer_1 ",
            " subscription_1 ",
            " pm_1 ",
            " price_monthly ",
            " monthly ",
            " active ",
            BillingPaymentKinds.Webhook,
            7.99m,
            " USD ",
            Now,
            Now.AddMonths(1),
            " evt_1 ",
            "{\"object\":\"payment\"}");

        Assert.Equal(BillingProviderNames.YooKassa, payment.Provider);
        Assert.Equal("payment_1", payment.ExternalPaymentId);
        Assert.Equal("customer_1", payment.ExternalCustomerId);
        Assert.Equal("subscription_1", payment.ExternalSubscriptionId);
        Assert.Equal("pm_1", payment.ExternalPaymentMethodId);
        Assert.Equal("price_monthly", payment.ExternalPriceId);
        Assert.Equal("monthly", payment.Plan);
        Assert.Equal("active", payment.Status);
        Assert.Equal(7.99m, payment.Amount);
        Assert.Equal("USD", payment.Currency);
        Assert.Equal("evt_1", payment.WebhookEventId);
        Assert.Equal("{\"object\":\"payment\"}", payment.ProviderMetadataJson);
    }
}
