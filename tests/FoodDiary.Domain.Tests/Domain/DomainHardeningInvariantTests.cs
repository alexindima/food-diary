using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.OpenFoodFacts.Domain.Entities;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class DomainHardeningInvariantTests {
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void WearableSyncEntry_RejectsNonFiniteValuesAndNormalizesDateToUtc() {
        var unspecified = new DateTime(2026, 4, 28, 17, 45, 0, DateTimeKind.Unspecified);
        var entry = WearableSyncEntry.Create(
            UserId.New(),
            WearableProvider.Fitbit,
            WearableDataType.Steps,
            unspecified,
            10_000);

        Assert.Multiple(
            () => Assert.Equal(DateTimeKind.Utc, entry.Date.Kind),
            () => Assert.Equal(new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), entry.Date),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => entry.UpdateValue(double.NaN)),
            () => Assert.Equal(10_000, entry.Value));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.1)]
    public void FastingTelemetryEvent_Create_WithInvalidActualDuration_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingTelemetryEvent.Create("fasting.completed", Now, actualDurationHours: value));
    }

    [Fact]
    public void OpenFoodFactsProduct_RejectsPersistenceOverflowAndInvalidNutritionAtomically() {
        OpenFoodFactsProduct product = CreateOpenFoodFactsProduct();

        Assert.Throws<ArgumentOutOfRangeException>(() => product.Update(
            new string('n', 513),
            brand: null,
            category: null,
            imageUrl: null,
            caloriesPer100G: 10,
            proteinsPer100G: 1,
            fatsPer100G: 1,
            carbsPer100G: 1,
            fiberPer100G: 1,
            Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => product.Update(
            "Changed",
            brand: null,
            category: null,
            imageUrl: null,
            caloriesPer100G: double.NaN,
            proteinsPer100G: 1,
            fatsPer100G: 1,
            carbsPer100G: 1,
            fiberPer100G: 1,
            Now));

        Assert.Multiple(
            () => Assert.Equal("Milk", product.Name),
            () => Assert.Equal(64, product.CaloriesPer100G),
            () => Assert.Equal(1, product.SearchHitCount));
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

    private static OpenFoodFactsProduct CreateOpenFoodFactsProduct() {
        return OpenFoodFactsProduct.Create(
            "4600000000001",
            "Milk",
            "Brand",
            "Dairy",
            imageUrl: null,
            caloriesPer100G: 64,
            proteinsPer100G: 3.2,
            fatsPer100G: 3.5,
            carbsPer100G: 4.8,
            fiberPer100G: 0,
            Now);
    }

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
}
