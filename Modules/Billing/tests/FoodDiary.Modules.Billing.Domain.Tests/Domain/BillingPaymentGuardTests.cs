using System.Globalization;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class BillingPaymentGuardTests {
    [Theory]
    [InlineData("9999999999999999.99")]
    [InlineData("-9999999999999999.99")]
    [InlineData("0")]
    [InlineData("1.2300")]
    public void Create_AcceptsStorageBoundaryAndTrailingZeros(string amount) {
        decimal value = decimal.Parse(amount, CultureInfo.InvariantCulture);
        Assert.Equal(value, Create(value).Amount);
    }

    [Theory]
    [InlineData("10000000000000000", "Value exceeds numeric(18,2) storage limits.")]
    [InlineData("-10000000000000000", "Value exceeds numeric(18,2) storage limits.")]
    [InlineData("1.005", "Value must have at most two fractional digits.")]
    [InlineData("1.015", "Value must have at most two fractional digits.")]
    [InlineData("-0.001", "Value must have at most two fractional digits.")]
    public void Create_RejectsAmountWithoutRounding(string amount, string message) {
        decimal value = decimal.Parse(amount, CultureInfo.InvariantCulture);
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Create(value));
        Assert.Equal("amount", exception.ParamName);
        Assert.Equal(RangeMessage("amount", message), exception.Message);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(" \t ", null)]
    [InlineData(" usd ", "USD")]
    [InlineData("xxx", "XXX")]
    public void Create_NormalizesCurrencyWithoutIsoLookup(string? input, string? expected) {
        BillingPayment payment = Create(amount: null, currency: input);
        Assert.Null(payment.Amount);
        Assert.Equal(expected, payment.Currency);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("U1D")]
    [InlineData("€UR")]
    [InlineData("uıd")]
    public void Create_RequiresExactlyThreeAsciiLetters(string currency) {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Create(amount: null, currency: currency));
        Assert.Equal("currency", exception.ParamName);
        Assert.Equal(ArgumentMessage("currency", "Currency code must contain exactly three ASCII letters."), exception.Message);
    }

    [Fact]
    public void Create_ChecksCurrencyLengthBeforeAsciiRule() {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Create(amount: null, currency: "1234"));
        Assert.Equal("currency", exception.ParamName);
        Assert.Equal(RangeMessage("currency", "Value must be at most 3 characters."), exception.Message);
    }

    private static BillingPayment Create(decimal? amount, string? currency = null) => BillingPayment.Create(
        userId: UserId.New(), billingSubscriptionId: null, provider: BillingProviderNames.Stripe,
        externalPaymentId: "payment", externalCustomerId: null, externalSubscriptionId: null,
        externalPaymentMethodId: null, externalPriceId: null, plan: null, status: "paid",
        kind: "payment", amount: amount, currency: currency, currentPeriodStartUtc: null,
        currentPeriodEndUtc: null, webhookEventId: null, providerMetadataJson: null);

    private static string RangeMessage(string paramName, string message) => new ArgumentOutOfRangeException(paramName, message).Message;

    private static string ArgumentMessage(string paramName, string message) => new ArgumentException(message, paramName).Message;
}
