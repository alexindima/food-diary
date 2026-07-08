using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using Stripe;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class BillingGatewayTests {
    private const string StripeTestApiKey = "stripe-test-api-key";

    [Fact]
    public void ConfigurableAccessor_ReturnsConfiguredProviderIgnoringCaseAndWhitespace() {
        var stripe = new StubBillingGateway(BillingProviderNames.Stripe);
        var paddle = new StubBillingGateway(BillingProviderNames.Paddle);
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [stripe, paddle],
            MsOptions.Create(new BillingOptions { Provider = " paddle " }));

        IBillingProviderGateway active = accessor.GetActiveProvider();
        IBillingProviderGateway? byName = accessor.GetProviderOrDefault(" STRIPE ");

        Assert.Same(paddle, active);
        Assert.Same(stripe, byName);
    }

    [Fact]
    public void ConfigurableAccessor_WhenProviderMissing_ThrowsProviderNotConfigured() {
        var accessor = new ConfigurableBillingProviderGatewayAccessor(
            [new StubBillingGateway(BillingProviderNames.Stripe)],
            MsOptions.Create(new BillingOptions { Provider = "Unknown" }));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => accessor.GetActiveProvider());

        Assert.Contains("not configured", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PaddleWebhook_WithValidSignature_MapsSubscriptionEvent() {
        var userId = Guid.NewGuid();
        string payload = JsonSerializer.Serialize(new {
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

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(payload, CreatePaddleSignature(payload, "secret"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
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

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync("{\"event_type\":\"subscription.updated\"}", "ts=1;h1=bad", CancellationToken.None);

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

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(userId, "buyer@example.com", "yearly", "ctm_123"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal("txn_123", result.Value.SessionId);
        Assert.Equal("https://checkout.paddle.com/txn_123", result.Value.Url);
        Assert.Equal("ctm_123", result.Value.CustomerId);
        Assert.Equal("pri_yearly", result.Value.PriceId);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("https://api.paddle.test/transactions", handler.LastRequest.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Contains("\"customer_id\":\"ctm_123\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"price_id\":\"pri_yearly\"", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaddleCreateCheckoutSession_WhenProviderNotConfigured_ReturnsProviderNotConfigured() {
        var gateway = new PaddleBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new PaddleOptions()));

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", "ctm_123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task PaddleCreateCheckoutSession_WithoutExistingCustomer_CreatesCustomerThenTransaction() {
        var handler = new RecordingHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK) {
                Content = JsonContent("""{ "data": { "id": "ctm_new" } }"""),
            },
            new HttpResponseMessage(HttpStatusCode.OK) {
                Content = JsonContent("""
                    {
                      "data": {
                        "id": "txn_new",
                        "checkout": { "url": "https://checkout.paddle.com/txn_new" }
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

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(userId, "buyer@example.com", "monthly", ExistingCustomerId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal("ctm_new", result.Value.CustomerId);
        Assert.Equal("txn_new", result.Value.SessionId);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("https://api.paddle.test/customers", handler.Requests[0].RequestUri?.ToString());
        Assert.Equal("https://api.paddle.test/transactions", handler.Requests[1].RequestUri?.ToString());
        Assert.Contains("\"email\":\"buyer@example.com\"", handler.RequestBodies[0], StringComparison.Ordinal);
        Assert.Contains("\"customer_id\":\"ctm_new\"", handler.RequestBodies[1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaddleCreateCheckoutSession_WhenCustomerCreationFails_ReturnsFailure() {
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

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", ExistingCustomerId: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task PaddleCreateCheckoutSession_WhenCheckoutUrlMissing_ReturnsProviderOperationFailure() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""{ "data": { "id": "txn_123", "checkout": { "url": "" } } }"""),
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

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", "ctm_123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Contains("checkout URL is missing", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaddleCreateCheckoutSession_WhenPaddleReturnsEmptyEnvelope_ReturnsProviderOperationFailure() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""{ "data": null }"""),
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

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", "ctm_123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Contains("empty response", result.Error.Message, StringComparison.OrdinalIgnoreCase);
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

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", "ctm_bad"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Contains("400 Bad Request", result.Error.Message, StringComparison.Ordinal);
        Assert.Contains("invalid customer", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaddleCreateCheckoutSession_WhenPlanUnsupported_ThrowsInvalidOperationException() {
        var gateway = new PaddleBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new PaddleOptions {
                ApiKey = "paddle-api-key",
                ApiBaseUrl = "https://api.paddle.test",
                PremiumMonthlyPriceId = "pri_monthly",
                PremiumYearlyPriceId = "pri_yearly",
                CheckoutUrl = "https://app.example/billing/paddle",
            }));

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gateway.CreateCheckoutSessionAsync(
                new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "weekly", "ctm_123"),
                CancellationToken.None));

        Assert.Contains("Unsupported billing plan", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaddleCreatePortalSession_WhenConfigured_MapsOverviewUrl() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""
                {
                  "data": {
                    "urls": {
                      "general": { "overview": "https://customers.paddle.com/portal" }
                    }
                  }
                }
                """),
        });
        var gateway = new PaddleBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(new PaddleOptions {
                ApiKey = "paddle-api-key",
                ApiBaseUrl = "https://api.paddle.test",
            }));

        Result<BillingPortalSessionModel> result = await gateway.CreatePortalSessionAsync(
            new BillingPortalSessionRequestModel("ctm_123"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal("https://customers.paddle.com/portal", result.Value.Url);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://api.paddle.test/customers/ctm_123/portal-sessions", handler.LastRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task PaddleCreatePortalSession_WhenProviderNotConfigured_ReturnsProviderNotConfigured() {
        var gateway = new PaddleBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new PaddleOptions()));

        Result<BillingPortalSessionModel> result = await gateway.CreatePortalSessionAsync(
            new BillingPortalSessionRequestModel("ctm_123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task PaddleCreatePortalSession_WhenOverviewUrlMissing_ReturnsProviderOperationFailure() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""{ "data": { "urls": { "general": { "overview": "" } } } }"""),
        });
        var gateway = new PaddleBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(new PaddleOptions {
                ApiKey = "paddle-api-key",
                ApiBaseUrl = "https://api.paddle.test",
            }));

        Result<BillingPortalSessionModel> result = await gateway.CreatePortalSessionAsync(
            new BillingPortalSessionRequestModel("ctm_123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Contains("portal URL is missing", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaddleCreatePortalSession_WhenProviderReturnsError_ReturnsFailure() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadGateway) {
            ReasonPhrase = "Bad Gateway",
            Content = new StringContent("upstream", Encoding.UTF8, "text/plain"),
        });
        var gateway = new PaddleBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(new PaddleOptions {
                ApiKey = "paddle-api-key",
                ApiBaseUrl = "https://api.paddle.test",
            }));

        Result<BillingPortalSessionModel> result = await gateway.CreatePortalSessionAsync(
            new BillingPortalSessionRequestModel("ctm_123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Contains("502 Bad Gateway", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PaddleWebhook_WhenPayloadOrSignatureMissing_ReturnsRequiredFailure() {
        PaddleBillingGateway gateway = CreateConfiguredPaddleWebhookGateway();

        Result<BillingWebhookEventModel?> missingPayload = await gateway.ParseWebhookEventAsync("", "ts=1;h1=signature", CancellationToken.None);
        Result<BillingWebhookEventModel?> missingSignature = await gateway.ParseWebhookEventAsync("{}", "", CancellationToken.None);

        Assert.True(missingPayload.IsFailure);
        Assert.Equal("Validation.Required", missingPayload.Error.Code);
        Assert.True(missingSignature.IsFailure);
        Assert.Equal("Validation.Required", missingSignature.Error.Code);
    }

    [Fact]
    public async Task PaddleWebhook_WhenProviderNotConfigured_ReturnsProviderNotConfigured() {
        var gateway = new PaddleBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new PaddleOptions()));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync("{}", "ts=1;h1=signature", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task PaddleWebhook_WhenSignatureHeaderMalformed_ReturnsValidationFailure() {
        PaddleBillingGateway gateway = CreateConfiguredPaddleWebhookGateway();

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync("{}", "ts=1", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
        Assert.Contains("malformed", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PaddleWebhook_WhenEventIsNotSubscription_ReturnsNullEvent() {
        string payload = JsonSerializer.Serialize(new {
            event_id = "evt_ignored",
            event_type = "transaction.completed",
            data = new {
                id = "txn_123",
                customer_id = "ctm_123",
            },
        });
        PaddleBillingGateway gateway = CreateConfiguredPaddleWebhookGateway();

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(payload, CreatePaddleSignature(payload, "secret"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task PaddleWebhook_WhenSubscriptionIdentifiersMissing_ReturnsNullEvent() {
        string payload = JsonSerializer.Serialize(new {
            event_id = "evt_missing_ids",
            event_type = "subscription.updated",
            data = new {
                id = "",
                customer_id = "ctm_123",
            },
        });
        PaddleBillingGateway gateway = CreateConfiguredPaddleWebhookGateway();

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(payload, CreatePaddleSignature(payload, "secret"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task PaddleWebhook_WhenItemsMissing_MapsNullPriceAndTrialDates() {
        var userId = Guid.NewGuid();
        string payload = JsonSerializer.Serialize(new {
            event_id = "evt_no_items",
            event_type = "subscription.updated",
            data = new {
                id = "sub_123",
                customer_id = "ctm_123",
                status = "active",
                current_billing_period = new {
                    starts_at = "2026-05-01T00:00:00Z",
                    ends_at = "2026-06-01T00:00:00Z",
                },
                custom_data = new {
                    user_id = userId.ToString(),
                    plan = "monthly",
                },
            },
        });
        PaddleBillingGateway gateway = CreateConfiguredPaddleWebhookGateway();

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(payload, CreatePaddleSignature(payload, "secret"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value.ExternalPriceId);
        Assert.Equal("monthly", result.Value.Plan);
        Assert.Null(result.Value.TrialStartUtc);
        Assert.Null(result.Value.TrialEndUtc);
    }

    [Fact]
    public async Task PaddleWebhook_WhenPriceIdUnknown_FallsBackToCustomPlan() {
        string payload = JsonSerializer.Serialize(new {
            event_id = "evt_unknown_price",
            event_type = "subscription.updated",
            data = new {
                id = "sub_123",
                customer_id = "ctm_123",
                status = "active",
                custom_data = new {
                    plan = "custom",
                },
                items = new[] {
                    new {
                        price = new { id = "pri_unknown" },
                    },
                },
            },
        });
        PaddleBillingGateway gateway = CreateConfiguredPaddleWebhookGateway();

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(payload, CreatePaddleSignature(payload, "secret"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal("pri_unknown", result.Value.ExternalPriceId);
        Assert.Equal("custom", result.Value.Plan);
    }

    [Fact]
    public async Task PaddleWebhook_WhenPayloadMalformed_ReturnsValidationFailure() {
        const string payload = """{ "event_type": "subscription.updated", "data": """;
        PaddleBillingGateway gateway = CreateConfiguredPaddleWebhookGateway();

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(payload, CreatePaddleSignature(payload, "secret"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }

    [Fact]
    public async Task PaddleWebhook_WhenYearlyPriceAndTrialDatesMissing_MapsYearlyPlanAndFallbackTrialEnd() {
        var userId = Guid.NewGuid();
        string payload = JsonSerializer.Serialize(new {
            event_id = "evt_yearly",
            event_type = "subscription.created",
            data = new {
                id = "sub_yearly",
                customer_id = "ctm_yearly",
                status = "trialing",
                next_billed_at = "2026-07-01T00:00:00Z",
                custom_data = new {
                    user_id = userId.ToString(),
                    plan = "ignored-by-price",
                },
                items = new[] {
                    new {
                        price = new { id = "pri_yearly" },
                    },
                },
            },
        });
        PaddleBillingGateway gateway = CreateConfiguredPaddleWebhookGateway();

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(payload, CreatePaddleSignature(payload, "secret"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal("yearly", result.Value.Plan);
        Assert.Equal("pri_yearly", result.Value.ExternalPriceId);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), result.Value.TrialEndUtc);
        Assert.Equal(userId, result.Value.UserId);
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

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
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
        Assert.Contains("\"plan\":\"monthly\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains(userId.ToString(), handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task YooKassaCreateCheckoutSession_WhenProviderNotConfigured_ReturnsFailure() {
        var gateway = new YooKassaBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new YooKassaOptions()));

        Assert.Equal(BillingProviderNames.YooKassa, gateway.Provider);

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", ExistingCustomerId: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task YooKassaCreateCheckoutSession_WhenProviderReturnsError_ReturnsFailure() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest) {
            ReasonPhrase = "Bad Request",
            Content = new StringContent("bad payment", Encoding.UTF8, "text/plain"),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", ExistingCustomerId: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Contains("400 Bad Request", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task YooKassaCreateCheckoutSession_WhenProviderReturnsNullJson_ReturnsEmptyResponseFailure() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("null"),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", ExistingCustomerId: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Contains("empty response", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task YooKassaCreateCheckoutSession_WhenConfirmationUrlMissing_ReturnsFailure() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""
                {
                  "id": "pay_123",
                  "status": "pending",
                  "paid": false,
                  "amount": { "value": "299.00", "currency": "RUB" },
                  "confirmation": { "confirmation_url": "" },
                  "metadata": {},
                  "created_at": "2026-05-06T00:00:00Z"
                }
                """),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", ExistingCustomerId: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Contains("confirmation URL is missing", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task YooKassaCreateCheckoutSession_WithYearlyPlan_UsesYearlyAmount() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""
                {
                  "id": "pay_123",
                  "status": "pending",
                  "paid": false,
                  "amount": { "value": "2990.00", "currency": "RUB" },
                  "confirmation": { "confirmation_url": "https://checkout.example/pay_123" },
                  "metadata": {},
                  "created_at": "2026-05-06T00:00:00Z"
                }
                """),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "yearly", ExistingCustomerId: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal("2990.00", result.Value.PriceId);
        Assert.Contains("\"value\":\"2990.00\"", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task YooKassaCreateCheckoutSession_WhenPlanUnsupported_ThrowsInvalidOperationException() {
        var gateway = new YooKassaBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(ValidYooKassaOptions()));

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gateway.CreateCheckoutSessionAsync(
                new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "weekly", ExistingCustomerId: null),
                CancellationToken.None));

        Assert.Contains("Unsupported billing plan", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task YooKassaCreatePortalSession_ReturnsProviderOperationFailure() {
        var gateway = new YooKassaBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingPortalSessionModel> result = await gateway.CreatePortalSessionAsync(
            new BillingPortalSessionRequestModel("customer"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Contains("does not provide", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task YooKassaWebhook_WhenPayloadBlank_ReturnsRequiredFailure() {
        var gateway = new YooKassaBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync("   ", signatureHeader: "", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    [Fact]
    public async Task YooKassaWebhook_WhenProviderNotConfigured_ReturnsProviderNotConfigured() {
        var gateway = new YooKassaBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new YooKassaOptions()));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            "{\"event\":\"payment.succeeded\",\"object\":{\"id\":\"pay_123\"}}",
            signatureHeader: "",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task YooKassaWebhook_WhenPayloadJsonInvalid_ReturnsWebhookValidationFailure() {
        var gateway = new YooKassaBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync("not-json", signatureHeader: "", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }

    [Theory]
    [InlineData("{\"event\":\"refund.succeeded\",\"object\":{\"id\":\"pay_123\"}}")]
    [InlineData("{\"event\":\"payment.succeeded\",\"object\":null}")]
    [InlineData("{\"event\":\"   \",\"object\":{\"id\":\"pay_123\"}}")]
    public async Task YooKassaWebhook_WhenNotificationIgnored_ReturnsNullWithoutFetchingPayment(string payload) {
        var handler = new RecordingHttpMessageHandler();
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(payload, signatureHeader: "", CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Null(result.Value);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task YooKassaWebhook_WhenFetchPaymentFails_ReturnsProviderFailure() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadGateway) {
            ReasonPhrase = "Bad Gateway",
            Content = new StringContent("upstream", Encoding.UTF8, "text/plain"),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            "{\"event\":\"payment.succeeded\",\"object\":{\"id\":\"pay_123\"}}",
            signatureHeader: "",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
        Assert.Contains("502 Bad Gateway", result.Error.Message, StringComparison.Ordinal);
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

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            "{\"event\":\"payment.succeeded\",\"object\":{\"id\":\"pay_123\",\"status\":\"succeeded\",\"paid\":true}}",
            signatureHeader: "",
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }

    [Fact]
    public async Task YooKassaWebhook_WhenPaymentSucceeded_MapsActiveSubscriptionEvent() {
        var userId = Guid.NewGuid();
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent($$"""
                {
                  "id": "pay_123",
                  "status": "succeeded",
                  "paid": true,
                  "amount": { "value": "299.00", "currency": "RUB" },
                  "payment_method": { "id": "pm_123" },
                  "metadata": {
                    "user_id": "{{userId}}",
                    "plan": "monthly"
                  },
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

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            "{\"event\":\"payment.succeeded\",\"object\":{\"id\":\"pay_123\"}}",
            signatureHeader: "",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("payment.succeeded:pay_123:succeeded", result.Value.EventId);
        Assert.Equal("payment.succeeded", result.Value.EventType);
        Assert.Equal(userId.ToString(), result.Value.ExternalCustomerId);
        Assert.Equal("pay_123", result.Value.ExternalSubscriptionId);
        Assert.Equal("pm_123", result.Value.ExternalPaymentMethodId);
        Assert.Equal("monthly", result.Value.Plan);
        Assert.Equal("active", result.Value.Status);
        Assert.Equal(new DateTime(2026, 5, 6, 0, 1, 0, DateTimeKind.Utc), result.Value.CurrentPeriodStartUtc);
        Assert.Equal(new DateTime(2026, 6, 6, 0, 1, 0, DateTimeKind.Utc), result.Value.CurrentPeriodEndUtc);
        Assert.Equal(299.00m, result.Value.Amount);
        Assert.Equal("RUB", result.Value.Currency);
        Assert.Equal(userId, result.Value.UserId);
    }

    [Fact]
    public async Task YooKassaWebhook_WhenPayloadEventIsCanceledButFetchedPaymentSucceeded_MapsVerifiedActiveEvent() {
        var userId = Guid.NewGuid();
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent($$"""
                {
                  "id": "pay_123",
                  "status": "succeeded",
                  "paid": true,
                  "amount": { "value": "299.00", "currency": "RUB" },
                  "payment_method": { "id": "pm_123" },
                  "metadata": {
                    "user_id": "{{userId}}",
                    "plan": "monthly"
                  },
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

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            "{\"event\":\"payment.canceled\",\"object\":{\"id\":\"pay_123\"}}",
            signatureHeader: "",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("payment.succeeded", result.Value.EventType);
        Assert.Equal("payment.succeeded:pay_123:succeeded", result.Value.EventId);
        Assert.Equal("active", result.Value.Status);
        Assert.Equal(userId, result.Value.UserId);
    }

    [Fact]
    public async Task YooKassaWebhook_WhenPaymentCanceledWithoutMetadata_MapsCanceledEventWithNullPeriod() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""
                {
                  "id": "pay_123",
                  "status": "canceled",
                  "paid": false,
                  "amount": { "value": "299.00", "currency": "RUB" },
                  "payment_method": { "id": "pm_123" },
                  "metadata": {},
                  "created_at": null,
                  "captured_at": null
                }
                """),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            "{\"event\":\"payment.canceled\",\"object\":{\"id\":\"pay_123\"}}",
            signatureHeader: "",
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal("payment.canceled", result.Value.EventType);
        Assert.Equal("canceled", result.Value.Status);
        Assert.Null(result.Value.CurrentPeriodStartUtc);
        Assert.Null(result.Value.CurrentPeriodEndUtc);
        Assert.Null(result.Value.UserId);
    }

    [Fact]
    public async Task YooKassaWebhook_WhenPlanUnknown_MapsActiveEventWithNullPeriodEnd() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""
                {
                  "id": "pay_123",
                  "status": "succeeded",
                  "paid": true,
                  "amount": { "value": "299.00", "currency": "RUB" },
                  "payment_method": { "id": "pm_123" },
                  "metadata": { "plan": "weekly" },
                  "captured_at": "2026-05-06T00:01:00Z"
                }
                """),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            "{\"event\":\"payment.succeeded\",\"object\":{\"id\":\"pay_123\"}}",
            signatureHeader: "",
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal("active", result.Value.Status);
        Assert.Equal("weekly", result.Value.Plan);
        Assert.Null(result.Value.CurrentPeriodEndUtc);
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

        Result<BillingRecurringPaymentModel> result = await gateway.CreateRecurringPaymentAsync(
            new BillingRecurringPaymentRequestModel(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "customer",
                "pm_123",
                "monthly",
                CurrentPeriodEndUtc: new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc),
                IdempotenceKey: "billing-renewal:test"),
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
        Assert.Equal("billing-renewal:test", handler.LastRequest.Headers.GetValues("Idempotence-Key").Single());
        Assert.Contains("\"renewal\":\"true\"", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"payment_method_id\":\"pm_123\"", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task YooKassaCreateRecurringPayment_WhenProviderNotConfigured_ReturnsFailure() {
        var gateway = new YooKassaBillingGateway(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new YooKassaOptions()));

        Result<BillingRecurringPaymentModel> result = await gateway.CreateRecurringPaymentAsync(
            new BillingRecurringPaymentRequestModel(Guid.NewGuid(), Guid.NewGuid(), "customer", "pm_123", "monthly", CurrentPeriodEndUtc: null, "renewal"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task YooKassaCreateRecurringPayment_WhenProviderReturnsError_ReturnsFailure() {
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest) {
            ReasonPhrase = "Bad Request",
            Content = new StringContent("bad renewal", Encoding.UTF8, "text/plain"),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingRecurringPaymentModel> result = await gateway.CreateRecurringPaymentAsync(
            new BillingRecurringPaymentRequestModel(Guid.NewGuid(), Guid.NewGuid(), "customer", "pm_123", "monthly", CurrentPeriodEndUtc: null, "renewal"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderOperationFailed", result.Error.Code);
    }

    [Fact]
    public async Task YooKassaCreateRecurringPayment_WithYearlyPlan_MapsYearlyPeriod() {
        var capturedAt = new DateTime(2026, 5, 6, 10, 0, 0, DateTimeKind.Utc);
        var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent($$"""
                {
                  "id": "pay_renewal",
                  "status": "succeeded",
                  "paid": true,
                  "amount": { "value": "2990.00", "currency": "RUB" },
                  "payment_method": { "id": "pm_123" },
                  "metadata": {},
                  "captured_at": "{{capturedAt:O}}"
                }
                """),
        });
        var gateway = new YooKassaBillingGateway(
            new HttpClient(handler),
            MsOptions.Create(ValidYooKassaOptions()));

        Result<BillingRecurringPaymentModel> result = await gateway.CreateRecurringPaymentAsync(
            new BillingRecurringPaymentRequestModel(Guid.NewGuid(), Guid.NewGuid(), "customer", "pm_123", "yearly", CurrentPeriodEndUtc: null, "renewal"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal(capturedAt.AddYears(1), result.Value.CurrentPeriodEndUtc);
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

        Result<BillingRecurringPaymentModel> result = await gateway.CreateRecurringPaymentAsync(
            new BillingRecurringPaymentRequestModel(Guid.NewGuid(), Guid.NewGuid(), "customer", "", "monthly", CurrentPeriodEndUtc: null, "renewal"),
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

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync("", "signature", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    [Fact]
    public async Task StripeWebhook_WhenProviderNotConfigured_ReturnsProviderNotConfiguredBeforePayloadValidation() {
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions()));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync("", "", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task StripeWebhook_WhenSignatureMissing_ReturnsRequiredFailure() {
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
        }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync("{}", "", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains("signatureHeader", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StripeWebhook_WhenSubscriptionUpdated_MapsSubscriptionEvent() {
        var userId = Guid.NewGuid();
        var currentPeriodStart = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var currentPeriodEnd = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        string payload = CreateStripeSubscriptionUpdatedPayload(userId, currentPeriodStart, currentPeriodEnd);
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
            PremiumMonthlyPriceId = "price_monthly",
            PremiumYearlyPriceId = "price_yearly",
        }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            payload,
            CreateStripeSignature(payload, "whsec_test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("evt_123", result.Value.EventId);
        Assert.Equal("customer.subscription.updated", result.Value.EventType);
        Assert.Equal("cus_123", result.Value.ExternalCustomerId);
        Assert.Equal("sub_123", result.Value.ExternalSubscriptionId);
        Assert.Equal("price_monthly", result.Value.ExternalPriceId);
        Assert.Equal("monthly", result.Value.Plan);
        Assert.Equal("active", result.Value.Status);
        Assert.True(result.Value.CancelAtPeriodEnd);
        Assert.Equal(currentPeriodStart.UtcDateTime, result.Value.CurrentPeriodStartUtc);
        Assert.Equal(currentPeriodEnd.UtcDateTime, result.Value.CurrentPeriodEndUtc);
        Assert.Equal(userId, result.Value.UserId);
    }

    [Fact]
    public async Task StripeWebhook_WhenSubscriptionCreated_MapsSubscriptionEvent() {
        var userId = Guid.NewGuid();
        var currentPeriodStart = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var currentPeriodEnd = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        string payload = CreateStripeSubscriptionPayload(
            "customer.subscription.created",
            userId,
            currentPeriodStart,
            currentPeriodEnd,
            priceId: "price_monthly");
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
            PremiumMonthlyPriceId = "price_monthly",
            PremiumYearlyPriceId = "price_yearly",
        }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            payload,
            CreateStripeSignature(payload, "whsec_test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal("customer.subscription.created", result.Value.EventType);
        Assert.Equal("sub_123", result.Value.ExternalSubscriptionId);
        Assert.Equal("price_monthly", result.Value.ExternalPriceId);
        Assert.Equal("monthly", result.Value.Plan);
        Assert.Equal(userId, result.Value.UserId);
    }

    [Fact]
    public async Task StripeWebhook_WhenSubscriptionDeletedWithYearlyPrice_MapsYearlySubscriptionEvent() {
        var userId = Guid.NewGuid();
        var currentPeriodStart = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var currentPeriodEnd = new DateTimeOffset(2027, 5, 1, 0, 0, 0, TimeSpan.Zero);
        string payload = CreateStripeSubscriptionPayload(
            "customer.subscription.deleted",
            userId,
            currentPeriodStart,
            currentPeriodEnd,
            priceId: "price_yearly");
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
            PremiumMonthlyPriceId = "price_monthly",
            PremiumYearlyPriceId = "price_yearly",
        }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            payload,
            CreateStripeSignature(payload, "whsec_test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal("customer.subscription.deleted", result.Value.EventType);
        Assert.Equal("price_yearly", result.Value.ExternalPriceId);
        Assert.Equal("yearly", result.Value.Plan);
        Assert.Equal(currentPeriodStart.UtcDateTime, result.Value.CurrentPeriodStartUtc);
        Assert.Equal(currentPeriodEnd.UtcDateTime, result.Value.CurrentPeriodEndUtc);
        Assert.Equal(userId, result.Value.UserId);
    }

    [Fact]
    public async Task StripeWebhook_WhenPriceIdUnknown_FallsBackToMetadataPlan() {
        var userId = Guid.NewGuid();
        var currentPeriodStart = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var currentPeriodEnd = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        string payload = CreateStripeSubscriptionPayload(
            "customer.subscription.updated",
            userId,
            currentPeriodStart,
            currentPeriodEnd,
            priceId: "price_unknown");
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
            PremiumMonthlyPriceId = "price_monthly",
            PremiumYearlyPriceId = "price_yearly",
        }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            payload,
            CreateStripeSignature(payload, "whsec_test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal("price_unknown", result.Value.ExternalPriceId);
        Assert.Equal("monthly", result.Value.Plan);
    }

    [Fact]
    public async Task StripeWebhook_WhenMetadataMissing_MapsNullUserAndPlanFromPrice() {
        var currentPeriodStart = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var currentPeriodEnd = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        string payload = JsonSerializer.Serialize(new {
            id = "evt_no_metadata",
            @object = "event",
            api_version = "2026-04-22.dahlia",
            type = "customer.subscription.updated",
            data = new {
                @object = new {
                    id = "sub_123",
                    @object = "subscription",
                    customer = "cus_123",
                    status = "active",
                    cancel_at_period_end = false,
                    items = new {
                        @object = "list",
                        data = new[] {
                            new {
                                id = "si_123",
                                @object = "subscription_item",
                                current_period_start = currentPeriodStart.ToUnixTimeSeconds(),
                                current_period_end = currentPeriodEnd.ToUnixTimeSeconds(),
                                price = new {
                                    id = "price_monthly",
                                    @object = "price",
                                },
                            },
                        },
                    },
                },
            },
        });
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
            PremiumMonthlyPriceId = "price_monthly",
            PremiumYearlyPriceId = "price_yearly",
        }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            payload,
            CreateStripeSignature(payload, "whsec_test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal("monthly", result.Value.Plan);
        Assert.Null(result.Value.UserId);
    }

    [Fact]
    public async Task StripeWebhook_WhenUnknownEvent_ReturnsNullEvent() {
        string payload = JsonSerializer.Serialize(new {
            id = "evt_ignored",
            @object = "event",
            api_version = "2026-04-22.dahlia",
            type = "invoice.paid",
            data = new {
                @object = new {
                    id = "in_123",
                    @object = "invoice",
                },
            },
        });
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
        }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            payload,
            CreateStripeSignature(payload, "whsec_test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task StripeWebhook_WhenCheckoutSessionIsNotSubscription_ReturnsNullEvent() {
        string payload = JsonSerializer.Serialize(new {
            id = "evt_checkout",
            @object = "event",
            api_version = "2026-04-22.dahlia",
            type = "checkout.session.completed",
            data = new {
                @object = new {
                    id = "cs_123",
                    @object = "checkout.session",
                    mode = "payment",
                    customer = "cus_123",
                },
            },
        });
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
        }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            payload,
            CreateStripeSignature(payload, "whsec_test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task StripeWebhook_WhenEventPayloadMalformed_ReturnsValidationFailure() {
        string payload = JsonSerializer.Serialize(new {
            id = "evt_malformed",
            @object = "event",
            type = "customer.subscription.updated",
            data = new {
                @object = new {
                    id = "sub_malformed",
                    @object = "subscription",
                },
            },
        });
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
        }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            payload,
            CreateStripeSignature(payload, "whsec_test"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }

    [Fact]
    public async Task StripeCreateCheckoutSession_WhenProviderNotConfigured_ReturnsProviderNotConfigured() {
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions()));

        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
            new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "monthly", ExistingCustomerId: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task StripeCreateCheckoutSession_WithoutExistingCustomer_CreatesCustomerAndCheckoutSession() {
        var userId = Guid.NewGuid();
        RecordingHttpMessageHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK) {
                Content = JsonContent("""{ "id": "cus_new", "object": "customer" }"""),
            },
            new HttpResponseMessage(HttpStatusCode.OK) {
                Content = JsonContent("""{ "id": "cs_new", "object": "checkout.session", "url": "https://checkout.stripe.test/cs_new" }"""),
            });

        await WithStripeHttpClientAsync(handler, async () => {
            StripeBillingGateway gateway = CreateConfiguredStripeGateway(handler);

            Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
                new BillingCheckoutSessionRequestModel(userId, "buyer@example.com", "monthly", ExistingCustomerId: null),
                CancellationToken.None).ConfigureAwait(false);

            Assert.True(result.IsSuccess, result.Error.Message);
            Assert.Equal("cs_new", result.Value.SessionId);
            Assert.Equal("https://checkout.stripe.test/cs_new", result.Value.Url);
            Assert.Equal("cus_new", result.Value.CustomerId);
            Assert.Equal("price_monthly", result.Value.PriceId);
            Assert.Equal("monthly", result.Value.Plan);
            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal("https://api.stripe.com/v1/customers", handler.Requests[0].RequestUri?.ToString());
            Assert.Equal("https://api.stripe.com/v1/checkout/sessions", handler.Requests[1].RequestUri?.ToString());
            Assert.Contains("email=buyer%40example.com", handler.RequestBodies[0], StringComparison.Ordinal);
            Assert.Contains($"metadata[user_id]={Uri.EscapeDataString(userId.ToString())}", handler.RequestBodies[0], StringComparison.Ordinal);
            Assert.Contains("customer=cus_new", handler.RequestBodies[1], StringComparison.Ordinal);
            Assert.Contains("line_items[0][price]=price_monthly", handler.RequestBodies[1], StringComparison.Ordinal);
        }).ConfigureAwait(true);
    }

    [Fact]
    public async Task StripeCreateCheckoutSession_WithExistingCustomer_CreatesCheckoutSessionOnly() {
        RecordingHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""{ "id": "cs_existing", "object": "checkout.session", "url": "https://checkout.stripe.test/cs_existing" }"""),
        });

        await WithStripeHttpClientAsync(handler, async () => {
            StripeBillingGateway gateway = CreateConfiguredStripeGateway(handler);

            Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(
                new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "yearly", ExistingCustomerId: "cus_existing"),
                CancellationToken.None).ConfigureAwait(false);

            Assert.True(result.IsSuccess, result.Error.Message);
            Assert.Equal("cs_existing", result.Value.SessionId);
            Assert.Equal("cus_existing", result.Value.CustomerId);
            Assert.Equal("price_yearly", result.Value.PriceId);
            HttpRequestMessage request = Assert.Single(handler.Requests);
            Assert.Equal("https://api.stripe.com/v1/checkout/sessions", request.RequestUri?.ToString());
            Assert.Contains("customer=cus_existing", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("line_items[0][price]=price_yearly", handler.LastRequestBody, StringComparison.Ordinal);
        }).ConfigureAwait(true);
    }

    [Fact]
    public async Task StripeCreatePortalSession_WhenProviderNotConfigured_ReturnsProviderNotConfigured() {
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions()));

        Result<BillingPortalSessionModel> result = await gateway.CreatePortalSessionAsync(
            new BillingPortalSessionRequestModel("cus_123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task StripeCreatePortalSession_WhenConfigured_ReturnsPortalUrl() {
        RecordingHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""{ "id": "bps_123", "object": "billing_portal.session", "url": "https://billing.stripe.test/session" }"""),
        });

        await WithStripeHttpClientAsync(handler, async () => {
            StripeBillingGateway gateway = CreateConfiguredStripeGateway(handler);

            Result<BillingPortalSessionModel> result = await gateway.CreatePortalSessionAsync(
                new BillingPortalSessionRequestModel("cus_123"),
                CancellationToken.None).ConfigureAwait(false);

            Assert.True(result.IsSuccess, result.Error.Message);
            Assert.Equal("https://billing.stripe.test/session", result.Value.Url);
            Assert.NotNull(handler.LastRequest);
            Assert.Equal("https://api.stripe.com/v1/billing_portal/sessions", handler.LastRequest.RequestUri?.ToString());
            Assert.Contains("customer=cus_123", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("return_url=https%3A%2F%2Fapp.example%2Fportal", handler.LastRequestBody, StringComparison.Ordinal);
        }).ConfigureAwait(true);
    }

    [Fact]
    public async Task StripeWebhook_WhenCheckoutSessionCompleted_FetchesSubscriptionAndMapsEvent() {
        var userId = Guid.NewGuid();
        DateTimeOffset currentPeriodStart = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset currentPeriodEnd = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        string payload = CreateStripeCheckoutSessionCompletedPayload(userId);
        RecordingHttpMessageHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent(CreateStripeSubscriptionJson(userId, currentPeriodStart, currentPeriodEnd, "price_monthly")),
        });

        await WithStripeHttpClientAsync(handler, async () => {
            StripeBillingGateway gateway = new(MsOptions.Create(new StripeOptions {
                SecretKey = StripeTestApiKey,
                WebhookSecret = "whsec_test",
                PremiumMonthlyPriceId = "price_monthly",
                PremiumYearlyPriceId = "price_yearly",
            }), CreateStripeClient(handler));

            Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
                payload,
                CreateStripeSignature(payload, "whsec_test"),
                CancellationToken.None).ConfigureAwait(false);

            Assert.True(result.IsSuccess, result.Error.Message);
            Assert.NotNull(result.Value);
            Assert.Equal("evt_checkout_completed", result.Value.EventId);
            Assert.Equal("checkout.session.completed", result.Value.EventType);
            Assert.Equal("cus_123", result.Value.ExternalCustomerId);
            Assert.Equal("sub_123", result.Value.ExternalSubscriptionId);
            Assert.Equal("price_monthly", result.Value.ExternalPriceId);
            Assert.Equal("monthly", result.Value.Plan);
            Assert.Equal(userId, result.Value.UserId);
            Assert.Equal("https://api.stripe.com/v1/subscriptions/sub_123", handler.LastRequest?.RequestUri?.ToString());
        }).ConfigureAwait(true);
    }

    [Fact]
    public async Task StripeCreateCheckoutSession_WhenPlanUnsupported_ThrowsInvalidOperationException() {
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            PremiumMonthlyPriceId = "price_monthly",
            PremiumYearlyPriceId = "price_yearly",
            SuccessUrl = "https://app.example/success",
            CancelUrl = "https://app.example/cancel",
            PortalReturnUrl = "https://app.example/portal",
        }));

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gateway.CreateCheckoutSessionAsync(
                new BillingCheckoutSessionRequestModel(Guid.NewGuid(), "buyer@example.com", "weekly", ExistingCustomerId: "cus_123"),
                CancellationToken.None));

        Assert.Contains("Unsupported billing plan", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StripePrivatePlanResolvers_MapConfiguredPricesAndBlankPrice() {
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            PremiumMonthlyPriceId = "price_monthly",
            PremiumYearlyPriceId = "price_yearly",
            SuccessUrl = "https://app.example/success",
            CancelUrl = "https://app.example/cancel",
            PortalReturnUrl = "https://app.example/portal",
        }));

        Assert.Equal("price_monthly", InvokePrivate<string>(gateway, "ResolvePriceId", "monthly"));
        Assert.Equal("price_yearly", InvokePrivate<string>(gateway, "ResolvePriceId", "yearly"));
        Assert.Null(InvokePrivate<string?>(gateway, "ResolvePlan", ""));
    }

    private static string CreatePaddleSignature(string payload, string secret) {
        const string timestamp = "1714996800";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        string hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}:{payload}"))).ToLowerInvariant();
        return $"ts={timestamp};h1={hash}";
    }

    private static PaddleBillingGateway CreateConfiguredPaddleWebhookGateway() =>
        new(
            new HttpClient(new RecordingHttpMessageHandler()),
            MsOptions.Create(new PaddleOptions {
                WebhookSecretKey = "secret",
                PremiumMonthlyPriceId = "pri_monthly",
                PremiumYearlyPriceId = "pri_yearly",
            }));

    private static string CreateStripeSignature(string payload, string secret) {
        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        string hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant();
        return $"t={timestamp},v1={hash}";
    }

    private static string CreateStripeSubscriptionUpdatedPayload(
        Guid userId,
        DateTimeOffset currentPeriodStart,
        DateTimeOffset currentPeriodEnd) =>
        CreateStripeSubscriptionPayload(
            "customer.subscription.updated",
            userId,
            currentPeriodStart,
            currentPeriodEnd,
            "price_monthly");

    private static string CreateStripeSubscriptionPayload(
        string eventType,
        Guid userId,
        DateTimeOffset currentPeriodStart,
        DateTimeOffset currentPeriodEnd,
        string priceId) =>
        JsonSerializer.Serialize(new {
            id = "evt_123",
            @object = "event",
            api_version = "2026-04-22.dahlia",
            type = eventType,
            data = new {
                @object = new {
                    id = "sub_123",
                    @object = "subscription",
                    customer = "cus_123",
                    status = "active",
                    cancel_at_period_end = true,
                    canceled_at = (long?)null,
                    trial_start = (long?)null,
                    trial_end = (long?)null,
                    metadata = new {
                        user_id = userId.ToString(),
                        plan = "monthly",
                    },
                    items = new {
                        @object = "list",
                        data = new[] {
                            new {
                                id = "si_123",
                                @object = "subscription_item",
                                current_period_start = currentPeriodStart.ToUnixTimeSeconds(),
                                current_period_end = currentPeriodEnd.ToUnixTimeSeconds(),
                                price = new {
                                    id = priceId,
                                    @object = "price",
                                },
                            },
                        },
                    },
                },
            },
        });

    private static string CreateStripeCheckoutSessionCompletedPayload(Guid userId) =>
        JsonSerializer.Serialize(new {
            id = "evt_checkout_completed",
            @object = "event",
            api_version = "2026-04-22.dahlia",
            type = "checkout.session.completed",
            data = new {
                @object = new {
                    id = "cs_123",
                    @object = "checkout.session",
                    mode = "subscription",
                    customer = "cus_123",
                    subscription = "sub_123",
                    metadata = new {
                        user_id = userId.ToString(),
                        plan = "monthly",
                    },
                },
            },
        });

    private static string CreateStripeSubscriptionJson(
        Guid userId,
        DateTimeOffset currentPeriodStart,
        DateTimeOffset currentPeriodEnd,
        string priceId) =>
        JsonSerializer.Serialize(new {
            id = "sub_123",
            @object = "subscription",
            customer = "cus_123",
            status = "active",
            cancel_at_period_end = false,
            canceled_at = (long?)null,
            trial_start = (long?)null,
            trial_end = (long?)null,
            metadata = new {
                user_id = userId.ToString(),
                plan = "monthly",
            },
            items = new {
                @object = "list",
                data = new[] {
                    new {
                        id = "si_123",
                        @object = "subscription_item",
                        current_period_start = currentPeriodStart.ToUnixTimeSeconds(),
                        current_period_end = currentPeriodEnd.ToUnixTimeSeconds(),
                        price = new {
                            id = priceId,
                            @object = "price",
                        },
                    },
                },
            },
        });

    private static StripeBillingGateway CreateConfiguredStripeGateway() =>
        new(MsOptions.Create(new StripeOptions {
            SecretKey = StripeTestApiKey,
            WebhookSecret = "whsec_test",
            PremiumMonthlyPriceId = "price_monthly",
            PremiumYearlyPriceId = "price_yearly",
            SuccessUrl = "https://app.example/success",
            CancelUrl = "https://app.example/cancel",
            PortalReturnUrl = "https://app.example/portal",
        }));

    private static StripeBillingGateway CreateConfiguredStripeGateway(RecordingHttpMessageHandler handler) =>
        new(
            MsOptions.Create(new StripeOptions {
                SecretKey = StripeTestApiKey,
                WebhookSecret = "whsec_test",
                PremiumMonthlyPriceId = "price_monthly",
                PremiumYearlyPriceId = "price_yearly",
                SuccessUrl = "https://app.example/success",
                CancelUrl = "https://app.example/cancel",
                PortalReturnUrl = "https://app.example/portal",
            }),
            CreateStripeClient(handler));

    private static IStripeClient CreateStripeClient(RecordingHttpMessageHandler handler) =>
        new StripeClient(
            StripeTestApiKey,
            httpClient: new SystemNetHttpClient(new HttpClient(handler)));

    private static async Task WithStripeHttpClientAsync(RecordingHttpMessageHandler handler, Func<Task> action) {
        IStripeClient previousClient = StripeConfiguration.StripeClient;
        string? previousApiKey = StripeConfiguration.ApiKey;
        try {
            StripeConfiguration.StripeClient = CreateStripeClient(handler);
            await action().ConfigureAwait(false);
        } finally {
            StripeConfiguration.StripeClient = previousClient;
            StripeConfiguration.ApiKey = previousApiKey;
        }
    }

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    private static T? InvokePrivate<T>(object instance, string methodName, params object?[] args) {
        System.Reflection.MethodInfo method = instance.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (T?)method.Invoke(instance, args);
    }

    private static YooKassaOptions ValidYooKassaOptions() =>
        new() {
            ShopId = "shop",
            SecretKey = "secret",
            ApiBaseUrl = "https://api.yookassa.test/v3",
            PremiumMonthlyAmount = "299",
            PremiumYearlyAmount = "2990",
            Currency = "RUB",
            ReturnUrl = "https://app.example/billing/return",
        };

    [ExcludeFromCodeCoverage]
    private sealed class StubBillingGateway(string provider) : IBillingProviderGateway {
        public string Provider => provider;
        public Task<Result<BillingCheckoutSessionModel>> CreateCheckoutSessionAsync(BillingCheckoutSessionRequestModel request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<BillingPortalSessionModel>> CreatePortalSessionAsync(BillingPortalSessionRequestModel request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<BillingWebhookEventModel?>> ParseWebhookEventAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHttpMessageHandler : HttpMessageHandler {
        private readonly Queue<HttpResponseMessage> _responses;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastRequestBody { get; private set; } = string.Empty;
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> RequestBodies { get; } = [];

        public RecordingHttpMessageHandler(params HttpResponseMessage[] responses) {
            _responses = new Queue<HttpResponseMessage>(
                responses.Length == 0
                    ? [new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent("{}") }]
                    : responses);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Requests.Add(request);
            RequestBodies.Add(LastRequestBody);
            return _responses.Count > 1
                ? _responses.Dequeue()
                : _responses.Peek();
        }
    }
}
