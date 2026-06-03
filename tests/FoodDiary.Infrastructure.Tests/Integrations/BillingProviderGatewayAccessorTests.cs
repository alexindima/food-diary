using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class BillingProviderGatewayAccessorTests {
    [Fact]
    public void GetActiveProvider_WithConfiguredProvider_ReturnsMatchingProvider() {
        var stripe = new StubBillingProviderGateway(BillingProviderNames.Stripe);
        var paddle = new StubBillingProviderGateway(BillingProviderNames.Paddle);
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [stripe, paddle],
            MsOptions.Create(new BillingOptions { Provider = " paddle " }));

        var provider = accessor.GetActiveProvider();

        Assert.Same(paddle, provider);
    }

    [Fact]
    public void GetActiveProvider_WithMissingProvider_Throws() {
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [new StubBillingProviderGateway(BillingProviderNames.Stripe)],
            MsOptions.Create(new BillingOptions { Provider = BillingProviderNames.YooKassa }));

        Assert.Throws<InvalidOperationException>(accessor.GetActiveProvider);
    }

    [Fact]
    public void GetProviderOrDefault_WithBlankProvider_ReturnsNull() {
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [new StubBillingProviderGateway(BillingProviderNames.Stripe)],
            MsOptions.Create(new BillingOptions()));

        Assert.Null(accessor.GetProviderOrDefault("   "));
    }

    [Fact]
    public void GetProviderOrDefault_WithDifferentCasing_ReturnsProvider() {
        var stripe = new StubBillingProviderGateway(BillingProviderNames.Stripe);
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [stripe],
            MsOptions.Create(new BillingOptions()));

        Assert.Same(stripe, accessor.GetProviderOrDefault(" STRIPE "));
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubBillingProviderGateway(string provider) : IBillingProviderGateway {
        public string Provider { get; } = provider;

        public Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(
            BillingCheckoutSessionRequestModel request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(
            BillingPortalSessionRequestModel request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(
            string payload,
            string signatureHeader,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
