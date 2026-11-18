using FoodDiary.Modules.Billing.Application.Common;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

[ExcludeFromCodeCoverage]
public sealed class TelegramStarsInvoiceMetadataTests {
    [Theory]
    [InlineData(null)]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"invoiceUrl":null}""")]
    [InlineData("""{"invoiceUrl":""}""")]
    [InlineData("""{"invoiceUrl":"/invoice/owned"}""")]
    [InlineData("""{"invoiceUrl":"http://t.me/$owned"}""")]
    [InlineData("""{"invoiceUrl":"https://t.me:444/$owned"}""")]
    [InlineData("""{"invoiceUrl":"https://t.me.example.test/$owned"}""")]
    [InlineData("""{"invoiceUrl":"https://user@t.me/$owned"}""")]
    [InlineData("""{"invoiceUrl":"https://t.me/$owned?from=quote"}""")]
    [InlineData("""{"invoiceUrl":"https://t.me/$owned#fragment"}""")]
    [InlineData("""{"invoiceUrl":"https://t.me/invoice/"}""")]
    [InlineData("""{"invoiceUrl":"https://t.me/invoice/owned/extra"}""")]
    public void ReadInvoiceUrl_RejectsMalformedMetadataAndInvalidInvoiceDestinations(string? metadata) {
        Assert.False(TelegramStarsInvoiceMetadata.TryReadInvoiceUrl(metadata, out string url));
        Assert.Empty(url);
    }

    [Theory]
    [InlineData("https://t.me/$owned-A_2=")]
    [InlineData("https://t.me/invoice/owned-A_2")]
    public void ReadInvoiceUrl_RoundTripsBothSupportedInvoiceFormats(string url) {
        string metadata = TelegramStarsInvoiceMetadata.Serialize(seconds: 300, url);

        Assert.True(TelegramStarsInvoiceMetadata.TryReadInvoiceUrl(metadata, out string restored));
        Assert.Equal(url, restored);
        Assert.True(TelegramStarsInvoiceMetadata.TryReadPeriod(metadata, out int seconds));
        Assert.Equal(300, seconds);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"subscriptionPeriodSeconds":"300"}""")]
    [InlineData("""{"subscriptionPeriodSeconds":300.1}""")]
    [InlineData("""{"subscriptionPeriodSeconds":2147483648}""")]
    [InlineData("""{"subscriptionPeriodSeconds":301}""")]
    public void ReadPeriod_RejectsCorruptAndUnsupportedQuotes(string metadata) =>
        Assert.False(TelegramStarsInvoiceMetadata.TryReadPeriod(metadata, out _));

    [Fact]
    public void ReadPeriod_UsesThirtyDaysForOrdersWithoutMetadata() {
        Assert.True(TelegramStarsInvoiceMetadata.TryReadPeriod(metadata: null, out int seconds));
        Assert.Equal(30 * 24 * 60 * 60, seconds);
    }
}
