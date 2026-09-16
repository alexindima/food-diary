using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Infrastructure.Providers;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using FoodDiary.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Services;

public sealed partial class BillingGatewayTests {
    [Fact]
    public void RecoveryGatewayCanBeResolvedFromProviderRegistration() {
        var services = new ServiceCollection();
        services.AddBillingIntegrations(new ConfigurationBuilder().Build());
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        Assert.IsType<PaddleNotificationRecoveryService>(scope.ServiceProvider.GetRequiredService<IPaddleNotificationRecoveryGateway>());
    }

    [Theory]
    [InlineData(false, "pay_123", HttpStatusCode.OK, "pay_123", "Billing.ProviderNotConfigured")]
    [InlineData(true, "../payment", HttpStatusCode.OK, "pay_123", "Billing.ProviderOperationFailed")]
    [InlineData(true, "pay_123", HttpStatusCode.BadGateway, "pay_123", "Billing.ProviderOperationFailed")]
    [InlineData(true, "pay_123", HttpStatusCode.OK, "pay_other", "Billing.ProviderOperationFailed")]
    public async Task RecurringLookupRejectsUntrustedOrUnavailablePaymentAsync(bool configured, string id, HttpStatusCode status, string returnedId, string errorCode) {
        using var http = new HttpClient(new RecordingHttpMessageHandler(new HttpResponseMessage(status) {
            Content = JsonContent("{\"id\":\"" + returnedId + "\",\"status\":\"succeeded\",\"paid\":true}"),
        }));
        var gateway = new YooKassaBillingGateway(http, MsOptions.Create(configured ? ValidYooKassaOptions() : new YooKassaOptions()));
        var request = new BillingRecurringPaymentRequestModel(Guid.NewGuid(), Guid.NewGuid(), "customer", "method", "monthly", DateTime.UtcNow, "key");
        Result<BillingRecurringPaymentModel> result = await gateway.GetRecurringPaymentAsync(id, request);
        Assert.True(result.IsFailure);
        Assert.Equal(errorCode, result.Error.Code);
    }

    [Theory]
    [InlineData("pending", false)]
    [InlineData("waiting_for_capture", false)]
    [InlineData("succeeded", false)]
    [InlineData("unknown", false)]
    public async Task YooKassaWebhookDoesNotActivateUnconfirmedPaymentAsync(string status, bool paid) {
        using var http = new HttpClient(new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent(JsonSerializer.Serialize(new { id = "pay_123", status, paid })),
        }));
        var gateway = new YooKassaBillingGateway(http, MsOptions.Create(ValidYooKassaOptions()));
        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync("""{"event":"payment.succeeded","object":{"id":"pay_123"}}""", "");
        if (status is "pending" or "waiting_for_capture") {
            Assert.True(result.IsSuccess);
            Assert.Null(result.Value);
        } else {
            Assert.True(result.IsFailure);
            Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
        }
    }

    [Theory]
    [InlineData("id")]
    [InlineData("customer")]
    [InlineData("currency")]
    [InlineData("amount_paid")]
    public async Task StripePaidInvoiceRejectsInvalidFinancialDetailsAsync(string field) {
        JsonNode payload = JsonNode.Parse(CreateStripeInvoicePayload(Guid.NewGuid(), "invoice.paid", "usd", 799, "price_monthly"))!;
        payload["data"]!["object"]![field] = string.Equals(field, "amount_paid", StringComparison.Ordinal)
            ? JsonValue.Create(-1) : JsonValue.Create("");
        string body = payload.ToJsonString();
        var gateway = new StripeBillingGateway(MsOptions.Create(new StripeOptions {
            SecretKey = "sk_test",
            WebhookSecret = "whsec_test",
            PremiumMonthlyPriceId = "price_monthly",
        }));
        Result<BillingWebhookEventModel?> result = await gateway.ParseWebhookEventAsync(body, CreateStripeSignature(body, "whsec_test"));
        Assert.True(result.IsFailure);
        Assert.Equal("Billing.WebhookValidationFailed", result.Error.Code);
    }
}
