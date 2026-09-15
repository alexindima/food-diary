using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingMetadataJsonBoundaryTests {
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
    public void JsonBackedValues_RejectInvalidJson() {
        Assert.Throws<ArgumentException>(() => BillingWebhookEvent.CreateReceived(
            BillingProviderNames.Stripe,
            "event",
            "payment",
            externalObjectId: null,
            DateTime.UtcNow,
            "{invalid",
            "{}"));
        Assert.Throws<ArgumentException>(() => CreatePayment(providerMetadataJson: "{invalid"));
    }

    [Fact]
    public void JsonBackedValues_RejectValidJsonAboveDomainLimit() {
        string oversizedJson = $"\"{new string('x', 65536)}\"";
        Assert.Throws<ArgumentOutOfRangeException>(() => BillingWebhookEvent.CreateReceived(
            BillingProviderNames.Stripe,
            "event",
            "payment",
            externalObjectId: null,
            DateTime.UtcNow,
            oversizedJson,
            "{}"));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePayment(providerMetadataJson: oversizedJson));
    }

    [Fact]
    public void JsonBackedValues_CountLeadingWhitespaceTowardDomainLimit() {
        string oversizedJson = new string(' ', 65536) + "{}";
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePayment(providerMetadataJson: oversizedJson));
    }
}
