using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class BillingInvariantTests {
    private static readonly UserId UserId = UserId.New();
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    private static BillingSubscription CreatePendingSubscription() {
        return BillingSubscription.CreatePending(
            UserId,
            BillingProviderNames.Stripe,
            "customer_1",
            "price_monthly",
            "monthly");
    }

    [Fact]
    public void BillingWebhookEvent_CreateProcessed_NormalizesValuesAndSetsProcessedStatus() {
        var processedAtLocal = new DateTime(2026, 4, 28, 12, 0, 0, DateTimeKind.Local);

        var webhookEvent = BillingWebhookEvent.CreateProcessed(
            provider: " STRIPE ",
            eventId: " evt_1 ",
            eventType: " subscription.updated ",
            externalObjectId: " sub_1 ",
            processedAtLocal,
            payloadJson: " {\"id\":\"evt_1\"} ");

        Assert.NotEqual(Guid.Empty, webhookEvent.Id);
        Assert.Equal(BillingProviderNames.Stripe, webhookEvent.Provider);
        Assert.Equal("evt_1", webhookEvent.EventId);
        Assert.Equal("subscription.updated", webhookEvent.EventType);
        Assert.Equal("sub_1", webhookEvent.ExternalObjectId);
        Assert.Equal("processed", webhookEvent.Status);
        Assert.Equal(processedAtLocal.ToUniversalTime(), webhookEvent.ProcessedAtUtc);
        Assert.Equal("{\"id\":\"evt_1\"}", webhookEvent.PayloadJson);
        Assert.Null(webhookEvent.ErrorMessage);
        Assert.Equal(processedAtLocal.ToUniversalTime(), webhookEvent.CreatedOnUtc);
    }

    [Fact]
    public void BillingWebhookEvent_CreateProcessed_WithBlankOptionalValues_StoresNulls() {
        var webhookEvent = BillingWebhookEvent.CreateProcessed(
            BillingProviderNames.Paddle,
            "evt_1",
            "subscription.updated",
            externalObjectId: " ",
            Now,
            payloadJson: " ");

        Assert.Equal(BillingProviderNames.Paddle, webhookEvent.Provider);
        Assert.Null(webhookEvent.ExternalObjectId);
        Assert.Null(webhookEvent.PayloadJson);
    }

    [Fact]
    public void BillingWebhookEvent_CreateProcessed_WithInvalidValues_Throws() {
        Assert.Throws<ArgumentException>(() =>
            BillingWebhookEvent.CreateProcessed("unknown", "evt_1", "type", null, Now, null));
        Assert.Throws<ArgumentException>(() =>
            BillingWebhookEvent.CreateProcessed(BillingProviderNames.Stripe, " ", "type", null, Now, null));
        Assert.Throws<ArgumentException>(() =>
            BillingWebhookEvent.CreateProcessed(BillingProviderNames.Stripe, "evt_1", " ", null, Now, null));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BillingWebhookEvent.CreateProcessed(BillingProviderNames.Stripe, "evt_1", "type", null, new DateTime(2026, 4, 28), null));
    }

    [Fact]
    public void BillingSubscription_CreatePending_NormalizesValuesAndSetsStatus() {
        var subscription = BillingSubscription.CreatePending(
            UserId,
            " PADDLE ",
            " customer_1 ",
            " price_1 ",
            " monthly ");

        Assert.NotEqual(Guid.Empty, subscription.Id);
        Assert.Equal(BillingProviderNames.Paddle, subscription.Provider);
        Assert.Equal("customer_1", subscription.ExternalCustomerId);
        Assert.Equal("price_1", subscription.ExternalPriceId);
        Assert.Equal("monthly", subscription.Plan);
        Assert.Equal(BillingSubscription.PendingCheckoutStatus, subscription.Status);
        Assert.NotEqual(default, subscription.CreatedOnUtc);
    }

    [Fact]
    public void BillingSubscription_CreatePending_WithBlankOptionalValues_StoresNulls() {
        var subscription = BillingSubscription.CreatePending(UserId, BillingProviderNames.Stripe, "customer_1", " ", " ");

        Assert.Null(subscription.ExternalPriceId);
        Assert.Null(subscription.Plan);
    }

    [Fact]
    public void BillingSubscription_CreatePending_WithInvalidValues_Throws() {
        Assert.Throws<ArgumentException>(() =>
            BillingSubscription.CreatePending(UserId.Empty, BillingProviderNames.Stripe, "customer_1", null, null));
        Assert.Throws<ArgumentException>(() =>
            BillingSubscription.CreatePending(UserId, "unknown", "customer_1", null, null));
        Assert.Throws<ArgumentException>(() =>
            BillingSubscription.CreatePending(UserId, BillingProviderNames.Stripe, " ", null, null));
    }

    [Fact]
    public void BillingSubscription_UpdateCheckoutContext_NormalizesAndSetsModified() {
        BillingSubscription subscription = CreatePendingSubscription();

        subscription.UpdateCheckoutContext(" yookassa ", " customer_2 ", " price_2 ", " annual ");

        Assert.Equal(BillingProviderNames.YooKassa, subscription.Provider);
        Assert.Equal("customer_2", subscription.ExternalCustomerId);
        Assert.Equal("price_2", subscription.ExternalPriceId);
        Assert.Equal("annual", subscription.Plan);
        Assert.NotNull(subscription.ModifiedOnUtc);
    }

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

    [Theory]
    [InlineData("trialing")]
    [InlineData("past_due")]
    public void BillingSubscription_Snapshot_WithRenewableStatuses_SchedulesNextBillingAttempt(string status) {
        BillingSubscription subscription = CreatePendingSubscription();

        subscription.ApplyProviderSnapshot(
            BillingProviderNames.Stripe,
            "sub_1",
            null,
            "price_monthly",
            "monthly",
            status,
            Now,
            Now.AddMonths(1),
            false,
            null,
            Now,
            Now.AddDays(7),
            "evt_1",
            Now,
            " {} ");

        Assert.Equal(Now.AddMonths(1), subscription.NextBillingAttemptUtc);
        Assert.Equal(Now, subscription.TrialStartUtc);
        Assert.Equal(Now.AddDays(7), subscription.TrialEndUtc);
        Assert.Equal("{}", subscription.ProviderMetadataJson);
        Assert.Equal(Now, subscription.LastSyncedAtUtc);
        Assert.Equal(Now, subscription.ModifiedOnUtc);
    }

    [Fact]
    public void BillingSubscription_Snapshot_WithNonRenewableStatus_DoesNotScheduleNextBillingAttempt() {
        BillingSubscription subscription = CreatePendingSubscription();

        subscription.ApplyProviderSnapshot(
            BillingProviderNames.Stripe,
            null,
            null,
            null,
            null,
            "canceled",
            null,
            Now.AddMonths(1),
            false,
            Now,
            null,
            null,
            "evt_1",
            Now);

        Assert.Null(subscription.NextBillingAttemptUtc);
        Assert.Equal(Now, subscription.CanceledAtUtc);
    }

    [Fact]
    public void BillingSubscription_Snapshot_WithInvalidTimestamps_Throws() {
        BillingSubscription subscription = CreatePendingSubscription();

        Assert.Throws<ArgumentOutOfRangeException>(() => subscription.ApplyProviderSnapshot(
            BillingProviderNames.Stripe,
            null,
            null,
            null,
            null,
            "active",
            new DateTime(2026, 4, 28),
            null,
            false,
            null,
            null,
            null,
            "evt_1",
            Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => subscription.ApplyProviderSnapshot(
            BillingProviderNames.Stripe,
            null,
            null,
            null,
            null,
            "active",
            null,
            null,
            false,
            null,
            null,
            null,
            "evt_1",
            new DateTime(2026, 4, 28)));
    }

    [Fact]
    public void BillingSubscription_MarkPremiumRoleManagedByBilling_IsIdempotent() {
        BillingSubscription subscription = CreatePendingSubscription();

        subscription.MarkPremiumRoleManagedByBilling(false, Now);
        Assert.Null(subscription.ModifiedOnUtc);

        subscription.MarkPremiumRoleManagedByBilling(true, Now);
        Assert.True(subscription.PremiumRoleManagedByBilling);
        Assert.Equal(Now, subscription.ModifiedOnUtc);
    }

    [Fact]
    public void BillingSubscription_MarkPremiumRoleManagedByBilling_WithUnspecifiedTimestamp_Throws() {
        BillingSubscription subscription = CreatePendingSubscription();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            subscription.MarkPremiumRoleManagedByBilling(true, new DateTime(2026, 4, 28)));
    }

    [Fact]
    public void BillingSubscription_MarkRenewalFailed_StoresPastDueState() {
        BillingSubscription subscription = CreatePendingSubscription();

        subscription.MarkRenewalFailed(Now.AddDays(1), " evt_failed ", Now, " {\"retry\":true} ");

        Assert.Equal("past_due", subscription.Status);
        Assert.Equal(Now.AddDays(1), subscription.NextBillingAttemptUtc);
        Assert.Equal("evt_failed", subscription.LastWebhookEventId);
        Assert.Equal(Now, subscription.LastSyncedAtUtc);
        Assert.Equal("{\"retry\":true}", subscription.ProviderMetadataJson);
        Assert.Equal(Now, subscription.ModifiedOnUtc);
    }

    [Fact]
    public void BillingSubscription_MarkRenewalSkippedForInaccessibleUser_StoresCanceledState() {
        BillingSubscription subscription = CreatePendingSubscription();

        subscription.MarkRenewalSkippedForInaccessibleUser(" evt_skipped ", Now, " {\"reason\":\"deleted\"} ");

        Assert.Equal("canceled", subscription.Status);
        Assert.False(subscription.CancelAtPeriodEnd);
        Assert.Equal(Now, subscription.CanceledAtUtc);
        Assert.Null(subscription.NextBillingAttemptUtc);
        Assert.Equal("evt_skipped", subscription.LastWebhookEventId);
        Assert.Equal(Now, subscription.LastSyncedAtUtc);
        Assert.Equal("{\"reason\":\"deleted\"}", subscription.ProviderMetadataJson);
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

    [Fact]
    public void BillingPayment_Create_WithPaddleProvider_NormalizesProviderName() {
        var payment = BillingPayment.Create(
            UserId,
            billingSubscriptionId: null,
            " PADDLE ",
            " payment_1 ",
            externalCustomerId: null,
            externalSubscriptionId: null,
            externalPaymentMethodId: null,
            externalPriceId: null,
            plan: null,
            status: "active",
            kind: BillingPaymentKinds.Webhook,
            amount: null,
            currency: null,
            currentPeriodStartUtc: null,
            currentPeriodEndUtc: null,
            webhookEventId: null,
            providerMetadataJson: null);

        Assert.Equal(BillingProviderNames.Paddle, payment.Provider);
    }

    [Fact]
    public void BillingPayment_Create_WithBlankOptionalValues_StoresNulls() {
        var payment = BillingPayment.Create(
            UserId,
            billingSubscriptionId: null,
            BillingProviderNames.Stripe,
            "payment_1",
            externalCustomerId: " ",
            externalSubscriptionId: null,
            externalPaymentMethodId: " ",
            externalPriceId: " ",
            plan: " ",
            status: "active",
            kind: BillingPaymentKinds.Webhook,
            amount: null,
            currency: " ",
            currentPeriodStartUtc: null,
            currentPeriodEndUtc: null,
            webhookEventId: " ",
            providerMetadataJson: " ");

        Assert.Null(payment.BillingSubscriptionId);
        Assert.Null(payment.ExternalCustomerId);
        Assert.Null(payment.ExternalSubscriptionId);
        Assert.Null(payment.ExternalPaymentMethodId);
        Assert.Null(payment.ExternalPriceId);
        Assert.Null(payment.Plan);
        Assert.Null(payment.Amount);
        Assert.Null(payment.Currency);
        Assert.Null(payment.CurrentPeriodStartUtc);
        Assert.Null(payment.CurrentPeriodEndUtc);
        Assert.Null(payment.WebhookEventId);
        Assert.Null(payment.ProviderMetadataJson);
    }

    [Fact]
    public void BillingPayment_Create_WithInvalidValues_Throws() {
        Assert.Throws<ArgumentException>(() =>
            BillingPayment.Create(UserId.Empty, null, BillingProviderNames.Stripe, "payment_1", null, null, null, null, null, "active", BillingPaymentKinds.Webhook, null, null, null, null, null, null));
        Assert.Throws<ArgumentException>(() =>
            BillingPayment.Create(UserId, null, "unknown", "payment_1", null, null, null, null, null, "active", BillingPaymentKinds.Webhook, null, null, null, null, null, null));
        Assert.Throws<ArgumentException>(() =>
            BillingPayment.Create(UserId, null, BillingProviderNames.Stripe, " ", null, null, null, null, null, "active", BillingPaymentKinds.Webhook, null, null, null, null, null, null));
        Assert.Throws<ArgumentException>(() =>
            BillingPayment.Create(UserId, null, BillingProviderNames.Stripe, "payment_1", null, null, null, null, null, " ", BillingPaymentKinds.Webhook, null, null, null, null, null, null));
        Assert.Throws<ArgumentException>(() =>
            BillingPayment.Create(UserId, null, BillingProviderNames.Stripe, "payment_1", null, null, null, null, null, "active", " ", null, null, null, null, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BillingPayment.Create(UserId, null, BillingProviderNames.Stripe, "payment_1", null, null, null, null, null, "active", BillingPaymentKinds.Webhook, null, null, new DateTime(2026, 4, 28), null, null, null));
    }
}
