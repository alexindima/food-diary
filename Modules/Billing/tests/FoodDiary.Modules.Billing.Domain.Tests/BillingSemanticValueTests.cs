using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Domain.ValueObjects;

namespace FoodDiary.Modules.Billing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingSemanticValueTests {
    [Theory]
    [InlineData(null, null)]
    [InlineData("12.345", null)]
    [InlineData(null, " usd ")]
    [InlineData("-12.345", "XYZ")]
    public void MoneyObservations_RetainPartialAndProviderCurrencyValues(string? amount, string? currency) {
        decimal? number = amount is null ? null : decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);
        var observation = BillingMoneyObservation.FromFields(number, currency);
        Assert.Multiple(() => Assert.Equal(number, observation.Amount?.Value), () => Assert.Equal(currency?.Trim().ToUpperInvariant(), observation.Currency?.Value));
    }

    [Fact]
    public void Money_DoesNotRoundOrInventMissingFields() {
        var financials = BillingPaymentFinancials.FromFields(amount: null, "USD", fee: -1.125m, payoutEarnings: 9.999m);
        Assert.Multiple(() => Assert.Null(financials.Transaction.Amount), () => Assert.Equal(-1.125m, financials.Fee?.Value), () => Assert.Null(financials.Payout.Currency), () => Assert.Equal(9.999m, financials.Payout.Amount?.Value));
        Assert.Throws<ArgumentOutOfRangeException>(() => BillingAmount.FromOptional(1.2345m));
        Assert.Throws<ArgumentException>(() => BillingCurrencyCode.FromOptional("US"));
    }

    [Fact]
    public void InternalIdentifiers_HaveDistinctTypesAndExplicitScalarRoundTrips() {
        var value = Guid.NewGuid();
        var subscription = (BillingSubscriptionId)value;
        var payment = (BillingPaymentId)value;
        var webhook = (BillingWebhookEventId)value;
        Assert.Multiple(() => Assert.Equal(value, subscription.Value), () => Assert.Equal(value, payment.Value), () => Assert.Equal(value, webhook.Value));
        Assert.DoesNotContain(typeof(BillingPaymentId).GetMethods(), method => string.Equals(method.Name, "op_Implicit", StringComparison.Ordinal) && method.ReturnType == typeof(BillingPaymentId));
    }

}
