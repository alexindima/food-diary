using System.Text.Json;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using FoodDiary.Results;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Services;

public sealed partial class BillingGatewayTests {
    [Theory]
    [InlineData("invoice.created")]
    [InlineData("invoice.payment_failed")]
    public async Task StripeInvoice_UnsupportedEvent_IsIgnoredWithoutProviderRequests(string eventType) {
        string payload = CreateStripeInvoicePayload(Guid.NewGuid(), eventType, "usd", 799, "price_other");
        Stripe.IStripeClient client = Substitute.For<Stripe.IStripeClient>();
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
        }), client);

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(
            payload, CreateStripeSignature(payload, "whsec_test"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.ToString() : null);
        Assert.Null(result.Value);
        Assert.Empty(client.ReceivedCalls());
    }

    [Theory]
    [InlineData("invoice.paid", "usd", 799, 7.99, "price_monthly", "monthly")]
    [InlineData("invoice.payment_succeeded", "usd", 799, 7.99, "price_yearly", "yearly")]
    [InlineData("invoice.paid", "jpy", 799, 799, "price_monthly", "monthly")]
    [InlineData("invoice.paid", "bhd", 7990, 7.99, "price_monthly", "monthly")]
    [InlineData("invoice.paid", "bhd", 7991, 7.991, "price_monthly", "monthly")]
    [InlineData("invoice.paid", "isk", 500, 5, "price_monthly", "monthly")]
    [InlineData("invoice.paid", "ugx", 500, 5, "price_monthly", "monthly")]
    [InlineData("invoice.paid", "usd", 0, 0, "price_monthly", "monthly")]
    public async Task StripePaidInvoice_MapsFinancialHistory(string eventType, string currency, long amount, decimal expectedAmount, string priceId, string plan) {
        var userId = Guid.NewGuid();
        string payload = CreateStripeInvoicePayload(userId, eventType, currency, amount, priceId);
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
            PremiumMonthlyPriceId = "price_monthly",
            PremiumYearlyPriceId = "price_yearly",
        }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(payload, CreateStripeSignature(payload, "whsec_test"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        BillingWebhookEventModel model = Assert.IsType<BillingWebhookEventModel>(result.Value);
        Assert.Equal("in_paid", model.ExternalPaymentId);
        Assert.Equal("sub_123", model.ExternalSubscriptionId);
        Assert.Equal("cus_123", model.ExternalCustomerId);
        Assert.Equal(userId, model.UserId);
        Assert.Equal(expectedAmount, model.Amount);
        Assert.Equal(currency.ToUpperInvariant(), model.Currency);
        Assert.Equal(plan, model.Plan);
        Assert.Equal("completed", model.Status);
        Assert.False(model.UpdatesSubscription);
        Assert.False(model.IsRenewal);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000).UtcDateTime, model.CurrentPeriodStartUtc);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1702678400).UtcDateTime, model.CurrentPeriodEndUtc);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000010).UtcDateTime, model.OccurredAtUtc);
    }

    [Theory]
    [InlineData("open", "sub_123")]
    [InlineData("paid", null)]
    public async Task StripeInvoice_WithoutPaidSubscription_IsIgnored(string status, string? subscriptionId) {
        string payload = CreateStripeInvoicePayload(Guid.NewGuid(), "invoice.paid", "usd", 799, "price_monthly", status, subscriptionId);
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions { SecretKey = "sk_test", WebhookSecret = "whsec_test" }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(payload, CreateStripeSignature(payload, "whsec_test"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task StripePaidInvoice_UnknownPrice_IsRejected() {
        string payload = CreateStripeInvoicePayload(Guid.NewGuid(), "invoice.paid", "usd", 799, "price_other");
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions { SecretKey = "sk_test", WebhookSecret = "whsec_test" }));

        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(payload, CreateStripeSignature(payload, "whsec_test"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }

    private static string CreateStripeInvoicePayload(Guid userId, string eventType, string currency, long amount,
        string priceId, string status = "paid", string? subscriptionId = "sub_123") =>
        JsonSerializer.Serialize(new {
            id = "evt_invoice",
            @object = "event",
            api_version = "2026-04-22.dahlia",
            type = eventType,
            created = 1700000020,
            data = new {
                @object = new {
                    id = "in_paid",
                    @object = "invoice",
                    customer = "cus_123",
                    status,
                    amount_paid = amount,
                    currency,
                    parent = new { type = "subscription_details", subscription_details = new { subscription = subscriptionId, metadata = new { user_id = userId.ToString(), }, }, },
                    status_transitions = new { paid_at = 1700000010, },
                    period_start = 1600000000,
                    period_end = 1602678400,
                    lines = new {
                        @object = "list",
                        data = new[] { new {
                        id = "il_premium", @object = "line_item",
                        pricing = new { type = "price_details", price_details = new { price = priceId, }, },
                        period = new { start = 1700000000, end = 1702678400, },
                    }, },
                    },
                },
            },
        });
}
