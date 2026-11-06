using System.Net;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Services;

public partial class BillingGatewayTests {
    [Fact]
    public async Task StarsGateway_RejectsPortalAndUnauthenticatedWebhookWithoutHttp() {
        using var http = new RecordingHttpMessageHandler();
        using var client = new HttpClient(http);
        TelegramStarsBillingGateway gateway = StarsGateway(client);

        ResultAssert.Failure(await gateway.CreatePortalSessionAsync(new BillingPortalSessionRequestModel("123:user")),
            "Billing.CustomerPortalUnavailable");
        ResultAssert.Failure(await gateway.ParseWebhookEventAsync("{}", "synthetic-signature"), "Billing.WebhookValidationFailed");

        Assert.Empty(http.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway, "ignored")]
    [InlineData(HttpStatusCode.OK, "{\"ok\":false,\"description\":\"private-provider-detail\"}")]
    [InlineData(HttpStatusCode.OK, "{\"ok\":true}")]
    [InlineData(HttpStatusCode.OK, "not-json")]
    [InlineData(HttpStatusCode.OK, "{\"ok\":true,\"result\":42}")]
    [InlineData(HttpStatusCode.OK, "{\"ok\":true,\"result\":\"http://t.me/invoice\"}")]
    [InlineData(HttpStatusCode.OK, "{\"ok\":true,\"result\":\"https://other.example/invoice\"}")]
    public async Task StarsInvoice_ProviderFailuresNeverReturnAnInvoiceOrExposeResponseDetails(HttpStatusCode status, string body) {
        using var http = new RecordingHttpMessageHandler(new HttpResponseMessage(status) { Content = JsonContent(body) });
        using var client = new HttpClient(http);
        TelegramStarsBillingGateway gateway = StarsGateway(client);

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(StarsCheckout());

        ResultAssert.Failure(result, "Billing.ProviderOperationFailed");
        Assert.DoesNotContain("private-provider-detail", result.Error.Message, StringComparison.Ordinal);
        Assert.Single(http.Requests);
    }

    [Theory]
    [InlineData("invalid", "charge")]
    [InlineData("0:user", "charge")]
    [InlineData("-1:user", "charge")]
    [InlineData("123:user", " ")]
    public async Task StarsCancellation_InvalidPayerOrMissingChargeNeverCallsTelegram(string payer, string charge) {
        using var http = new RecordingHttpMessageHandler();
        using var client = new HttpClient(http);

        Result<BillingCancellationSnapshotModel> result = await StarsGateway(client).CancelSubscriptionAsync(
            new BillingCancellationRequestModel("invoice", payer, charge));

        ResultAssert.Failure(result, "Billing.CancellationUnavailable");
        Assert.Empty(http.Requests);
    }

    [Theory]
    [InlineData("{\"ok\":true,\"result\":false}", "123:synthetic", "Billing.ProviderOperationFailed", 1)]
    [InlineData("{\"ok\":false}", "123:synthetic", "Billing.ProviderOperationFailed", 1)]
    [InlineData("{}", "", "Billing.ProviderNotConfigured", 0)]
    public async Task StarsCancellation_RequiresConfiguredTransportAndPositiveConfirmation(string body, string token, string error, int calls) {
        using var http = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(body) });
        using var client = new HttpClient(http);

        Result<BillingCancellationSnapshotModel> result = await StarsGateway(client, token).CancelSubscriptionAsync(
            new BillingCancellationRequestModel("invoice", "123:user", "charge"));

        ResultAssert.Failure(result, error);
        Assert.Equal(calls, http.Requests.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StarsInvoice_TransportFailureOrTimeoutReturnsSanitizedFailure(bool timeout) {
        using var handler = new StarsFailureHandler(timeout);
        using var client = new HttpClient(handler);

        Result<BillingCheckoutSessionModel> result = await StarsGateway(client).CreateCheckoutSessionAsync(StarsCheckout());

        ResultAssert.Failure(result, "Billing.ProviderOperationFailed");
        Assert.DoesNotContain("synthetic", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StarsInvoice_CallerCancellationPropagates() {
        using var handler = new StarsFailureHandler(timeout: true);
        using var client = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            StarsGateway(client).CreateCheckoutSessionAsync(StarsCheckout(), cancellation.Token));
    }

    private static TelegramStarsBillingGateway StarsGateway(HttpClient client, string token = "123:synthetic") =>
        new(client, Options.Create(new TelegramStarsOptions {
            CheckoutEnabled = true,
            PremiumPrice = 500,
            BotToken = token,
            BotApiSecret = new string('s', 32),
        }), TimeProvider.System);

    private static BillingCheckoutSessionRequestModel StarsCheckout() =>
        new(Guid.NewGuid(), string.Empty, "monthly", ExistingCustomerId: null, "order", TelegramUserId: 123);

    [ExcludeFromCodeCoverage]
    private sealed class StarsFailureHandler(bool timeout) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(timeout
                ? new OperationCanceledException("https://api.telegram.org/bot123:synthetic")
                : new HttpRequestException("https://api.telegram.org/bot123:synthetic"));
    }
}
