using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class BillingPublicConfigProviderTests {
    [Fact]
    public void GetPublicConfig_WithValidStripePrimaryAndOtherProviders_ListsAvailableProviders() {
        var provider = CreateProvider(
            billing: new BillingOptions { Provider = BillingProviderNames.Stripe },
            stripe: ValidStripeOptions(),
            paddle: ValidPaddleOptions(),
            yooKassa: ValidYooKassaOptions());

        var config = provider.GetPublicConfig();

        Assert.Equal(BillingProviderNames.Stripe, config.Provider);
        Assert.Equal([BillingProviderNames.Stripe, BillingProviderNames.Paddle, BillingProviderNames.YooKassa], config.AvailableProviders);
        Assert.Equal("paddle-client-token", config.PaddleClientToken);
    }

    [Fact]
    public void GetPublicConfig_WithPaddlePrimary_NormalizesProviderAndTrimsClientToken() {
        var provider = CreateProvider(
            billing: new BillingOptions { Provider = " paddle " },
            stripe: new StripeOptions(),
            paddle: ValidPaddleOptions(clientSideToken: " paddle-token "),
            yooKassa: new YooKassaOptions());

        var config = provider.GetPublicConfig();

        Assert.Equal("paddle", config.Provider);
        Assert.Equal([BillingProviderNames.Paddle], config.AvailableProviders);
        Assert.Equal("paddle-token", config.PaddleClientToken);
    }

    [Fact]
    public void GetPublicConfig_WithInvalidConfigurations_ReturnsNoAvailableProviders() {
        var provider = CreateProvider(
            billing: new BillingOptions { Provider = BillingProviderNames.Stripe },
            stripe: new StripeOptions { SecretKey = "sk" },
            paddle: new PaddleOptions(),
            yooKassa: new YooKassaOptions());

        var config = provider.GetPublicConfig();

        Assert.Equal(BillingProviderNames.Stripe, config.Provider);
        Assert.Empty(config.AvailableProviders);
        Assert.Null(config.PaddleClientToken);
    }

    private static BillingPublicConfigProvider CreateProvider(
        BillingOptions billing,
        StripeOptions stripe,
        PaddleOptions paddle,
        YooKassaOptions yooKassa) {
        return new BillingPublicConfigProvider(
            MsOptions.Create(billing),
            MsOptions.Create(stripe),
            MsOptions.Create(paddle),
            MsOptions.Create(yooKassa));
    }

    private static StripeOptions ValidStripeOptions() {
        return new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec",
            PremiumMonthlyPriceId = "price-month",
            PremiumYearlyPriceId = "price-year",
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel",
            PortalReturnUrl = "https://example.com/portal"
        };
    }

    private static PaddleOptions ValidPaddleOptions(string clientSideToken = "paddle-client-token") {
        return new PaddleOptions {
            ApiKey = "paddle-api-key",
            ClientSideToken = clientSideToken,
            PremiumMonthlyPriceId = "pri_month",
            PremiumYearlyPriceId = "pri_year",
            CheckoutUrl = "https://checkout.paddle.com"
        };
    }

    private static YooKassaOptions ValidYooKassaOptions() {
        return new YooKassaOptions {
            ShopId = "shop",
            SecretKey = "secret",
            PremiumMonthlyAmount = "199.00",
            PremiumYearlyAmount = "1990.00",
            Currency = "RUB",
            ReturnUrl = "https://example.com/return"
        };
    }
}
