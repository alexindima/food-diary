using System.Net;
using System.Text;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using FoodDiary.Results;
using Stripe;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class BillingGatewayResilienceTests {
    private const string StripeTestApiKey = "stripe-test-api-key";

    [Fact]
    public async Task PaddleCheckout_WhenCustomerRequestFails_MapsProviderOperationFailure() {
        var handler = new ScriptedHttpMessageHandler(
            PaddleResponse("""{ "data": [] }"""),
            new HttpRequestException("sensitive network details"));
        var gateway = new PaddleBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(ValidPaddleOptions()));

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", ExistingCustomerId: null),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code),
            () => Assert.DoesNotContain("sensitive network details", result.Error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task PaddleCheckout_WhenCallerCancels_PropagatesCancellation() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var gateway = new PaddleBillingGateway(
            new HttpClient(new ScriptedHttpMessageHandler(new TaskCanceledException())),
            MsOptions.Create(ValidPaddleOptions()));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            gateway.CreateCheckoutSessionAsync(
                new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", "ctm_123"),
                cancellation.Token));
    }

    [Fact]
    public async Task PaddlePortal_WhenTransportFails_MapsProviderOperationFailure() {
        var gateway = new PaddleBillingGateway(
            new HttpClient(new ScriptedHttpMessageHandler(new HttpRequestException("sensitive network details"))),
            MsOptions.Create(ValidPaddleOptions()));

        Result<BillingPortalSessionModel> result = await gateway.CreatePortalSessionAsync(
            new BillingPortalSessionRequestModel("ctm_123"),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code),
            () => Assert.DoesNotContain("sensitive network details", result.Error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task YooKassaCheckout_WhenTransportFails_MapsProviderOperationFailure() {
        var gateway = new YooKassaBillingGateway(
            new HttpClient(new ScriptedHttpMessageHandler(new HttpRequestException("sensitive network details"))),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", ExistingCustomerId: null),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code),
            () => Assert.DoesNotContain("sensitive network details", result.Error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task YooKassaCheckout_WhenCallerCancels_PropagatesCancellation() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var gateway = new YooKassaBillingGateway(
            new HttpClient(new ScriptedHttpMessageHandler(new TaskCanceledException())),
            MsOptions.Create(ValidYooKassaOptions()));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            gateway.CreateCheckoutSessionAsync(
                new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", ExistingCustomerId: null),
                cancellation.Token));
    }

    [Fact]
    public async Task StripeCheckout_WhenSessionUrlIsMissing_ReturnsProviderOperationFailure() {
        StripeBillingGateway gateway = CreateStripeGateway(new ScriptedHttpMessageHandler(
            StripeResponse("""{ "id": "cs_missing_url", "object": "checkout.session", "url": null }""")));

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", "cus_existing"),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code),
            () => Assert.Contains("URL is invalid", result.Error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task StripeCheckout_WhenTransportFails_MapsProviderOperationFailure() {
        StripeBillingGateway gateway = CreateStripeGateway(
            new ScriptedHttpMessageHandler(new HttpRequestException("sensitive network details")));

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", "cus_existing"),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code),
            () => Assert.DoesNotContain("sensitive network details", result.Error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task StripeCheckout_WhenCallerCancels_PropagatesCancellation() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        StripeBillingGateway gateway = CreateStripeGateway(
            new ScriptedHttpMessageHandler(new TaskCanceledException()));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            gateway.CreateCheckoutSessionAsync(
                new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", "cus_existing"),
                cancellation.Token));
    }

    [Fact]
    public async Task StripePortal_WhenUrlIsMissing_ReturnsProviderOperationFailure() {
        StripeBillingGateway gateway = CreateStripeGateway(new ScriptedHttpMessageHandler(
            StripeResponse("""{ "id": "bps_missing_url", "object": "billing_portal.session", "url": null }""")));

        Result<BillingPortalSessionModel> result = await gateway.CreatePortalSessionAsync(
            new BillingPortalSessionRequestModel("cus_123"),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code),
            () => Assert.Contains("URL is invalid", result.Error.Message, StringComparison.Ordinal));
    }

    private static PaddleOptions ValidPaddleOptions() => new() {
        ApiKey = "paddle-api-key",
        ApiBaseUrl = "https://api.paddle.test",
        PremiumMonthlyPriceId = "pri_monthly",
        PremiumYearlyPriceId = "pri_yearly",
        CheckoutUrl = "https://app.example/billing/paddle",
    };

    private static YooKassaOptions ValidYooKassaOptions() => new() {
        ShopId = "shop",
        SecretKey = "secret",
        ApiBaseUrl = "https://api.yookassa.test/v3",
        PremiumMonthlyAmount = "299",
        PremiumYearlyAmount = "2990",
        Currency = "RUB",
        ReturnUrl = "https://app.example/billing/return",
    };

    private static StripeBillingGateway CreateStripeGateway(HttpMessageHandler handler) =>
        new(
            MsOptions.Create(new StripeOptions {
                SecretKey = StripeTestApiKey,
                PremiumMonthlyPriceId = "price_monthly",
                PremiumYearlyPriceId = "price_yearly",
                SuccessUrl = "https://app.example/success",
                CancelUrl = "https://app.example/cancel",
                PortalReturnUrl = "https://app.example/portal",
            }),
            new StripeClient(
                StripeTestApiKey,
                httpClient: new SystemNetHttpClient(new HttpClient(handler))));

    private static HttpResponseMessage PaddleResponse(string json) =>
        new(HttpStatusCode.OK) { Content = JsonContent(json) };

    private static HttpResponseMessage StripeResponse(string json) =>
        new(HttpStatusCode.OK) { Content = JsonContent(json) };

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    [ExcludeFromCodeCoverage]
    private sealed class ScriptedHttpMessageHandler(params object[] outcomes) : HttpMessageHandler {
        private readonly Queue<object> _outcomes = new(outcomes);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            object outcome = _outcomes.Count > 1 ? _outcomes.Dequeue() : _outcomes.Peek();
            return outcome is Exception exception
                ? Task.FromException<HttpResponseMessage>(exception)
                : Task.FromResult((HttpResponseMessage)outcome);
        }
    }
}
