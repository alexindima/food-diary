using System.Net;
using System.Text;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using FoodDiary.Results;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class YooKassaRecurringPaymentTests {
    private static readonly DateTime PeriodEnd = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("pending", false, "pending")]
    [InlineData("waiting_for_capture", false, "pending")]
    [InlineData("canceled", false, "canceled")]
    [InlineData("succeeded", true, "active")]
    [InlineData("succeeded", false, null)]
    [InlineData("unknown", false, null)]
    public async Task RecurringPayment_DistinguishesUnresolvedAndFinalOutcomes(string providerStatus, bool paid, string? expectedStatus) {
        using var http = new PaymentHttpHandler("pay_pending", providerStatus, paid);
        using var client = new HttpClient(http);
        YooKassaBillingGateway gateway = CreateGateway(client);

        Result<BillingRecurringPaymentModel> result = await gateway.CreateRecurringPaymentAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(HttpMethod.Post, http.Method);
        Assert.Equal("attempt-key", http.IdempotenceKey);
        if (expectedStatus is null) {
            Assert.True(result.IsFailure);
        } else {
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : string.Empty);
            Assert.Multiple(
                () => Assert.Equal(expectedStatus, result.Value.Status),
                () => Assert.Equal("pay_pending", result.Value.PaymentId),
                () => Assert.Equal(string.Equals(expectedStatus, "active", StringComparison.Ordinal) ? PeriodEnd.AddMonths(1) : PeriodEnd,
                    result.Value.CurrentPeriodEndUtc));
        }
    }

    [Theory]
    [InlineData("pay_pending", "pay_pending", true)]
    [InlineData("pay_pending", "another_payment", false)]
    [InlineData("../payments", "pay_pending", false)]
    public async Task PendingPaymentVerification_UsesOnlyGetAndChecksIdentity(string requestedId, string responseId, bool success) {
        using var http = new PaymentHttpHandler(responseId, "succeeded", paid: true);
        using var client = new HttpClient(http);
        YooKassaBillingGateway gateway = CreateGateway(client);

        Result<BillingRecurringPaymentModel> result = await gateway.GetRecurringPaymentAsync(requestedId, CreateRequest(), CancellationToken.None);

        Assert.Equal(success, result.IsSuccess);
        Assert.Null(http.IdempotenceKey);
        if (requestedId.Contains('/', StringComparison.Ordinal)) {
            Assert.Null(http.Method);
        } else {
            Assert.Multiple(
                () => Assert.Equal(HttpMethod.Get, http.Method),
                () => Assert.Equal("/v3/payments/pay_pending", http.Path));
        }
        if (success) {
            Assert.Equal(PeriodEnd, result.Value.CurrentPeriodStartUtc);
        }
    }

    private static BillingRecurringPaymentRequestModel CreateRequest() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "customer", "pm_saved", "monthly", PeriodEnd, "attempt-key");

    [Theory]
    [InlineData("pending")]
    [InlineData("waiting_for_capture")]
    public async Task UnresolvedPaymentWebhook_DoesNotCancelSubscription(string status) {
        using var http = new PaymentHttpHandler("pay_pending", status, paid: false);
        using var client = new HttpClient(http);
        YooKassaBillingGateway gateway = CreateGateway(client);

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            """{"event":"payment.waiting_for_capture","object":{"id":"pay_pending"}}""", string.Empty, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    private static YooKassaBillingGateway CreateGateway(HttpClient client) => new(client, Options.Create(new YooKassaOptions {
        ShopId = "shop",
        SecretKey = "test-secret",
        ApiBaseUrl = "https://api.yookassa.test/v3",
        PremiumMonthlyAmount = "299",
        PremiumYearlyAmount = "2990",
        Currency = "RUB",
        ReturnUrl = "https://app.example/billing/return",
    }));

    [ExcludeFromCodeCoverage]
    private sealed class PaymentHttpHandler(string paymentId, string status, bool paid) : HttpMessageHandler {
        public HttpMethod? Method { get; private set; }
        public string? Path { get; private set; }
        public string? IdempotenceKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Method = request.Method;
            Path = request.RequestUri?.AbsolutePath;
            IdempotenceKey = request.Headers.TryGetValues("Idempotence-Key", out IEnumerable<string>? values) ? values.Single() : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent($$"""
                    {"id":"{{paymentId}}","status":"{{status}}","paid":{{(paid ? "true" : "false")}},
                     "amount":{"value":"299.00","currency":"RUB"},"payment_method":{"id":"pm_saved"},
                     "created_at":"2026-08-01T10:00:00Z","captured_at":"2026-08-01T10:00:00Z"}
                    """, Encoding.UTF8, "application/json"),
            });
        }
    }
}
