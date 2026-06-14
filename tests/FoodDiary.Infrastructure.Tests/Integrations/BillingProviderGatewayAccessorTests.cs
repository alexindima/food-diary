using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class BillingProviderGatewayAccessorTests {
    [Fact]
    public void GetActiveProvider_WithConfiguredProvider_ReturnsMatchingProvider() {
        IBillingProviderGateway stripe = CreateGateway(BillingProviderNames.Stripe);
        IBillingProviderGateway paddle = CreateGateway(BillingProviderNames.Paddle);
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [stripe, paddle],
            MsOptions.Create(new BillingOptions { Provider = " paddle " }));

        IBillingProviderGateway provider = accessor.GetActiveProvider();

        Assert.Same(paddle, provider);
    }

    [Fact]
    public void GetActiveProvider_WithMissingProvider_Throws() {
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [CreateGateway(BillingProviderNames.Stripe)],
            MsOptions.Create(new BillingOptions { Provider = BillingProviderNames.YooKassa }));

        Assert.Throws<InvalidOperationException>(accessor.GetActiveProvider);
    }

    [Fact]
    public void GetProviderOrDefault_WithBlankProvider_ReturnsNull() {
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [CreateGateway(BillingProviderNames.Stripe)],
            MsOptions.Create(new BillingOptions()));

        Assert.Null(accessor.GetProviderOrDefault("   "));
    }

    [Fact]
    public void GetProviderOrDefault_WithDifferentCasing_ReturnsProvider() {
        IBillingProviderGateway stripe = CreateGateway(BillingProviderNames.Stripe);
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [stripe],
            MsOptions.Create(new BillingOptions()));

        Assert.Same(stripe, accessor.GetProviderOrDefault(" STRIPE "));
    }

    private static IBillingProviderGateway CreateGateway(string provider) {
        IBillingProviderGateway gateway = Substitute.For<IBillingProviderGateway>();
        gateway.Provider.Returns(provider);
        return gateway;
    }
}
