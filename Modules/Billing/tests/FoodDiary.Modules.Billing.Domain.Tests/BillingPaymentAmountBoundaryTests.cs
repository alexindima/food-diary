using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingPaymentAmountBoundaryTests {
    private static BillingPayment CreatePayment(
        decimal? amount = null,
        string? currency = null,
        string? providerMetadataJson = null) => BillingPayment.Create(
            UserId.New(),
            billingSubscriptionId: null,
            BillingProviderNames.Stripe,
            "payment",
            externalCustomerId: null,
            externalSubscriptionId: null,
            externalPaymentMethodId: null,
            externalPriceId: null,
            plan: null,
            status: "active",
            BillingPaymentKinds.Webhook,
            amount,
            currency,
            currentPeriodStartUtc: null,
            currentPeriodEndUtc: null,
            webhookEventId: null,
            providerMetadataJson);

    [Fact]
    public void BillingPayment_ValidatesStoragePrecisionAndCurrency() {
        BillingPayment payment = CreatePayment(amount: 12.34m, currency: " usd ");

        Assert.Equal("USD", payment.Currency);
        Assert.Equal(12.345m, CreatePayment(amount: 12.345m).Amount);
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePayment(amount: 12.3456m));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePayment(amount: 10_000_000_000_000_000m));
        Assert.Throws<ArgumentException>(() => CreatePayment(currency: "US1"));
        Assert.Throws<ArgumentException>(() => CreatePayment(currency: "US"));
    }
}
