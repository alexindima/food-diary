using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class BillingGatewayTests {
    [Fact]
    public void ConfigurableAccessor_ReturnsConfiguredProviderIgnoringCaseAndWhitespace() {
        var stripe = new StubBillingGateway(BillingProviderNames.Stripe);
        var paddle = new StubBillingGateway(BillingProviderNames.Paddle);
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [stripe, paddle],
            MsOptions.Create(new BillingOptions { Provider = " paddle " }));

        var active = accessor.GetActiveProvider();
        var byName = accessor.GetProviderOrDefault(" STRIPE ");

        Assert.Same(paddle, active);
        Assert.Same(stripe, byName);
    }

    [Fact]
    public void ConfigurableAccessor_WhenProviderMissing_ThrowsProviderNotConfigured() {
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [new StubBillingGateway(BillingProviderNames.Stripe)],
            MsOptions.Create(new BillingOptions { Provider = "Unknown" }));

        var ex = Assert.Throws<InvalidOperationException>(() => accessor.GetActiveProvider());

        Assert.Contains("not configured", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PaddleWebhook_WithValidSignature_MapsSubscriptionEvent() {
        var userId = Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new {
            event_id = "evt_123",
            event_type = "subscription.updated",
            data = new {
                id = "sub_123",
                customer_id = "ctm_123",
                status = "active",
                current_billing_period = new {
                    starts_at = "2026-05-01T00:00:00Z",
                    ends_at = "2026-06-01T00:00:00Z",
                },
                scheduled_change = new {
                    action = "cancel",
                },
                custom_data = new {
                    user_id = userId.ToString(),
                    plan = "monthly",
                },
                items = new[] {
                    new {
                        price = new { id = "pri_monthly" },
                        trial_dates = new {
                            starts_at = "2026-04-20T00:00:00Z",
                            ends_at = "2026-05-01T00:00:00Z",
                        },
                    },
                },
            },
        });
        var gateway = new PaddleBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new PaddleOptions {
                WebhookSecretKey = "secret",
                PremiumMonthlyPriceId = "pri_monthly",
                PremiumYearlyPriceId = "pri_yearly",
            }));

        var result = await gateway.ParseWebhookEventAsync(payload, CreatePaddleSignature(payload, "secret"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("evt_123", result.Value.EventId);
        Assert.Equal("ctm_123", result.Value.ExternalCustomerId);
        Assert.Equal("sub_123", result.Value.ExternalSubscriptionId);
        Assert.Equal("pri_monthly", result.Value.ExternalPriceId);
        Assert.Equal("monthly", result.Value.Plan);
        Assert.True(result.Value.CancelAtPeriodEnd);
        Assert.Equal(userId, result.Value.UserId);
    }

    [Fact]
    public async Task PaddleWebhook_WithInvalidSignature_ReturnsValidationFailure() {
        var gateway = new PaddleBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new PaddleOptions {
                WebhookSecretKey = "secret",
                PremiumMonthlyPriceId = "pri_monthly",
                PremiumYearlyPriceId = "pri_yearly",
            }));

        var result = await gateway.ParseWebhookEventAsync("{\"event_type\":\"subscription.updated\"}", "ts=1;h1=bad", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }

    [Fact]
    public async Task PaddleCreateCheckoutSession_WithExistingCustomer_SendsTransactionAndMapsCheckoutUrl() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""
                {
                  "data": {
                    "id": "txn_123",
                    "checkout": { "url": "https://checkout.paddle.com/txn_123" }
                  }
                }
                """),
        });
        var gateway = new PaddleBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(new PaddleOptions {
                ApiKey = "paddle-api-key",
                ApiBaseUrl = "https://api.paddle.test",
                PremiumMonthlyPriceId = "pri_monthly",
                PremiumYearlyPriceId = "pri_yearly",
                CheckoutUrl = "https://app.example/billing/paddle",
            }));
        var userId = Guid.NewGuid();

        var result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(userId, "buyer@example.com", "yearly", "ctm_123"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("txn_123", result.Value.SessionId);
        Assert.Equal("https://checkout.paddle.com/txn_123", result.Value.Url);
        Assert.Equal("ctm_123", result.Value.CustomerId);
        Assert.Equal("pri_yearly", result.Value.PriceId);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("https://api.paddle.test/transactions", handler.LastRequest.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Contains("\"customer_id\":\"ctm_123\"", handler.LastRequestBody);
        Assert.Contains("\"price_id\":\"pri_yearly\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task PaddleCreateCheckoutSession_WhenProviderReturnsError_MapsProviderOperationFailure() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest) {
            ReasonPhrase = "Bad Request",
            Content = new StringContent("{\"error\":\"invalid customer\"}", Encoding.UTF8, "application/json"),
        });
        var gateway = new PaddleBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(new PaddleOptions {
                ApiKey = "paddle-api-key",
                ApiBaseUrl = "https://api.paddle.test",
                PremiumMonthlyPriceId = "pri_monthly",
                PremiumYearlyPriceId = "pri_yearly",
                CheckoutUrl = "https://app.example/billing/paddle",
            }));

        var result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", "ctm_bad"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Contains("400 Bad Request", result.Error.Message);
        Assert.Contains("invalid customer", result.Error.Message);
    }

    [Fact]
    public async Task YooKassaCreateCheckoutSession_SendsPaymentRequestWithIdempotenceKeyAndMapsConfirmationUrl() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""
                {
                  "id": "pay_123",
                  "status": "pending",
                  "paid": false,
                  "amount": { "value": "299.00", "currency": "RUB" },
                  "confirmation": { "confirmation_url": "https://checkout.example/pay_123" },
                  "metadata": {},
                  "created_at": "2026-05-06T00:00:00Z"
                }
                """),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(new YooKassaOptions {
                ShopId = "shop",
                SecretKey = "secret",
                ApiBaseUrl = "https://api.yookassa.test/v3",
                PremiumMonthlyAmount = "299",
                PremiumYearlyAmount = "2990",
                Currency = "RUB",
                ReturnUrl = "https://app.example/billing/return",
            }));
        var userId = Guid.NewGuid();

        var result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(userId, "buyer@example.com", "monthly", ExistingCustomerId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("pay_123", result.Value.SessionId);
        Assert.Equal("https://checkout.example/pay_123", result.Value.Url);
        Assert.Equal("299.00", result.Value.PriceId);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("https://api.yookassa.test/v3/payments", handler.LastRequest.RequestUri?.ToString());
        Assert.True(handler.LastRequest.Headers.Contains("Idempotence-Key"));
        Assert.Equal("Basic", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Contains("\"plan\":\"monthly\"", handler.LastRequestBody);
        Assert.Contains(userId.ToString(), handler.LastRequestBody);
    }

    [Fact]
    public async Task YooKassaWebhook_WhenVerificationFetchReturnsDifferentPayment_ReturnsValidationFailure() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""
                {
                  "id": "pay_other",
                  "status": "succeeded",
                  "paid": true,
                  "amount": { "value": "299.00", "currency": "RUB" },
                  "payment_method": { "id": "pm_123" },
                  "metadata": {},
                  "created_at": "2026-05-06T00:00:00Z",
                  "captured_at": "2026-05-06T00:01:00Z"
                }
                """),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(new YooKassaOptions {
                ShopId = "shop",
                SecretKey = "secret",
                ApiBaseUrl = "https://api.yookassa.test/v3",
                PremiumMonthlyAmount = "299",
                PremiumYearlyAmount = "2990",
                ReturnUrl = "https://app.example/billing/return",
            }));

        var result = await gateway.ParseWebhookEventAsync(
            "{\"event\":\"payment.succeeded\",\"object\":{\"id\":\"pay_123\",\"status\":\"succeeded\",\"paid\":true}}",
            signatureHeader: "",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }

    [Fact]
    public async Task YooKassaCreateRecurringPayment_WhenPaymentSucceeded_MapsActiveSubscriptionPeriod() {
        var capturedAt = new DateTime(2026, 5, 6, 10, 0, 0, DateTimeKind.Utc);
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent($$"""
                {
                  "id": "pay_renewal",
                  "status": "succeeded",
                  "paid": true,
                  "amount": { "value": "299.00", "currency": "RUB" },
                  "payment_method": { "id": "pm_123" },
                  "metadata": {},
                  "created_at": "2026-05-06T09:59:00Z",
                  "captured_at": "{{capturedAt:O}}"
                }
                """),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(new YooKassaOptions {
                ShopId = "shop",
                SecretKey = "secret",
                ApiBaseUrl = "https://api.yookassa.test/v3",
                PremiumMonthlyAmount = "299",
                PremiumYearlyAmount = "2990",
                Currency = "RUB",
                ReturnUrl = "https://app.example/billing/return",
            }));

        var result = await gateway.CreateRecurringPaymentAsync(
            new BillingRecurringPaymentRequestModel(
                Guid.NewGuid(),
                "customer",
                "pm_123",
                "monthly",
                CurrentPeriodEndUtc: new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("pay_renewal", result.Value.PaymentId);
        Assert.Equal("pm_123", result.Value.PaymentMethodId);
        Assert.Equal("active", result.Value.Status);
        Assert.Equal(new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc), result.Value.CurrentPeriodStartUtc);
        Assert.Equal(new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc), result.Value.CurrentPeriodEndUtc);
        Assert.Equal(299.00m, result.Value.Amount);
        Assert.Equal("RUB", result.Value.Currency);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://api.yookassa.test/v3/payments", handler.LastRequest.RequestUri?.ToString());
        Assert.Contains("\"renewal\":\"true\"", handler.LastRequestBody);
        Assert.Contains("\"payment_method_id\":\"pm_123\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task YooKassaCreateRecurringPayment_WhenPaymentMethodMissing_ReturnsRequiredFailure() {
        var gateway = new YooKassaBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new YooKassaOptions {
                ShopId = "shop",
                SecretKey = "secret",
                ApiBaseUrl = "https://api.yookassa.test/v3",
                PremiumMonthlyAmount = "299",
                PremiumYearlyAmount = "2990",
                Currency = "RUB",
                ReturnUrl = "https://app.example/billing/return",
            }));

        var result = await gateway.CreateRecurringPaymentAsync(
            new BillingRecurringPaymentRequestModel(Guid.NewGuid(), "customer", "", "monthly", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    [Fact]
    public async Task StripeWebhook_WhenPayloadMissing_ReturnsRequiredFailureBeforeSignatureValidation() {
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
        }));

        var result = await gateway.ParseWebhookEventAsync("", "signature", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    private static string CreatePaddleSignature(string payload, string secret) {
        const string timestamp = "1714996800";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}:{payload}"))).ToLowerInvariant();
        return $"ts={timestamp};h1={hash}";
    }

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    private sealed class StubBillingGateway(string provider) : IBillingProviderGateway {
        public string Provider => provider;
        public Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(BillingCheckoutSessionRequestModel request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(BillingPortalSessionRequestModel request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingHttpMessageHandler(HttpResponseMessage? response = null) : HttpMessageHandler {
        private readonly HttpResponseMessage _response = response ?? new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("{}"),
        };

        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return _response;
        }
    }
}
