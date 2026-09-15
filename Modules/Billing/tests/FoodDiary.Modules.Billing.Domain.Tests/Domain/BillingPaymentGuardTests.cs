using System.Globalization;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class BillingPaymentGuardTests {
    [Theory]
    [InlineData("9999999999999999.99")]
    [InlineData("-9999999999999999.99")]
    [InlineData("0")]
    [InlineData("1.2300")]
    [InlineData("7.991")]
    [InlineData("-0.001")]
    [InlineData("9999999999999999.999")]
    [InlineData("-9999999999999999.999")]
    public void Create_AcceptsStorageBoundaryAndTrailingZeros(string amount) {
        decimal value = decimal.Parse(amount, CultureInfo.InvariantCulture);
        Assert.Equal(value, Create(value).Amount);
    }

    [Theory]
    [InlineData("10000000000000000", "Value exceeds numeric(19,3) storage limits.")]
    [InlineData("-10000000000000000", "Value exceeds numeric(19,3) storage limits.")]
    [InlineData("1.0005", "Value must have at most three fractional digits.")]
    [InlineData("1.0015", "Value must have at most three fractional digits.")]
    [InlineData("-0.0001", "Value must have at most three fractional digits.")]
    public void Create_RejectsAmountWithoutRounding(string amount, string message) {
        decimal value = decimal.Parse(amount, CultureInfo.InvariantCulture);
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Create(value));
        Assert.Equal("amount", exception.ParamName);
        Assert.Equal(RangeMessage("amount", message), exception.Message);
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("tax")]
    [InlineData("fee")]
    [InlineData("earnings")]
    [InlineData("payoutEarnings")]
    public void ApplyProviderResult_RejectsExcessPrecisionBeforeChangingAnyFields(string field) {
        BillingPayment payment = Create(7.991m, "BHD");
        decimal Value(string name) => string.Equals(field, name, StringComparison.Ordinal) ? 0.0001m : 0.001m;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => payment.ApplyProviderResult(
            billingSubscriptionId: null, externalCustomerId: "new_customer", externalSubscriptionId: null,
            externalPaymentMethodId: null, externalPriceId: null, plan: null, status: "refunded", kind: BillingPaymentKinds.Transaction,
            amount: Value("amount"), currency: "USD", currentPeriodStartUtc: null, currentPeriodEndUtc: null,
            webhookEventId: "new_event", providerMetadataJson: null,
            tax: Value("tax"), fee: Value("fee"), earnings: Value("earnings"), payoutEarnings: Value("payoutEarnings")));

        Assert.Equal(field, exception.ParamName);
        Assert.Multiple(
            () => Assert.Equal(7.991m, payment.Amount),
            () => Assert.Equal("BHD", payment.Currency),
            () => Assert.Equal("paid", payment.Status),
            () => Assert.Null(payment.ExternalCustomerId),
            () => Assert.Null(payment.WebhookEventId),
            () => Assert.Null(payment.ModifiedOnUtc));
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
