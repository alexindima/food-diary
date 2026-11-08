using System.Net;
using System.Text.Json;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using FoodDiary.Testing.Assertions;
using FoodDiary.Results;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Services;

public partial class BillingGatewayTests {
    [Theory]
    [InlineData(false, 2592000, "30 days")]
    [InlineData(true, 300, "5 minutes")]
    public async Task StarsInvoice_UsesIntegerXtrAndEnvironmentPeriod(bool testEnvironment, int periodSeconds, string periodLabel) {
        using var http = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("{\"ok\":true,\"result\":\"https://t.me/$invoice-owned\"}"),
        });
        using var client = new HttpClient(http);
        var gateway = new TelegramStarsBillingGateway(client, Options.Create(new TelegramStarsOptions {
            CheckoutEnabled = true,
            PremiumPrice = 500,
            BotToken = "123:synthetic",
            BotApiSecret = new string('s', 32),
            UseTestEnvironment = testEnvironment,
        }), TimeProvider.System);
        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(new BillingCheckoutSessionRequestModel(Guid.NewGuid(),
            string.Empty, "monthly", ExistingCustomerId: null, "stable-order", TelegramUserId: 123));
        ResultAssert.Success(result);
        using var payload = JsonDocument.Parse(http.LastRequestBody!);
        JsonElement root = payload.RootElement;
        Assert.Multiple(() => {
            Assert.Equal("XTR", root.GetProperty("currency").GetString());
            Assert.Equal(periodSeconds, root.GetProperty("subscription_period").GetInt32());
            Assert.Equal(periodSeconds, result.Value.SubscriptionPeriodSeconds);
            Assert.Contains(periodLabel, root.GetProperty("title").GetString(), StringComparison.Ordinal);
            Assert.Contains(periodLabel, root.GetProperty("description").GetString(), StringComparison.Ordinal);
            if (testEnvironment) {
                Assert.Contains("Test", root.GetProperty("title").GetString(), StringComparison.Ordinal);
            }
            Assert.Equal(500, Assert.Single(root.GetProperty("prices").EnumerateArray()).GetProperty("amount").GetInt32());
            Assert.Equal(result.Value.SessionId, root.GetProperty("payload").GetString());
            Assert.True(result.Value.SessionId.Length <= 128);
            Assert.Equal(500, result.Value.Amount);
            Assert.StartsWith("123:", result.Value.CustomerId, StringComparison.Ordinal);
            Assert.Contains(testEnvironment ? "/test/createInvoiceLink" : "/123:synthetic/createInvoiceLink",
                http.LastRequest!.RequestUri!.AbsolutePath.Replace("bot123", "123", StringComparison.Ordinal), StringComparison.Ordinal);
        });
    }

    [Theory]
    [InlineData(false, "monthly", 123)]
    [InlineData(true, "yearly", 123)]
    [InlineData(true, "monthly", 0)]
    public async Task StarsInvoice_InvalidOrDisabledCheckoutNeverCallsTelegram(bool enabled, string plan, long actor) {
        using var http = new RecordingHttpMessageHandler();
        using var client = new HttpClient(http);
        var gateway = new TelegramStarsBillingGateway(client, Options.Create(new TelegramStarsOptions {
            CheckoutEnabled = enabled,
            BotToken = "123:synthetic",
            BotApiSecret = new string('s', 32),
        }), TimeProvider.System);
        Result<BillingCheckoutSessionModel> result = await gateway.CreateCheckoutSessionAsync(new BillingCheckoutSessionRequestModel(Guid.NewGuid(),
            string.Empty, plan, ExistingCustomerId: null, "order", actor));
        ResultAssert.Failure(result);
        Assert.Empty(http.Requests);
    }

    [Fact]
    public async Task StarsCancellation_UsesFirstChargeAndPreservesPaidPeriodEvenWithCheckoutDisabled() {
        using var http = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent("{\"ok\":true,\"result\":true}") });
        using var client = new HttpClient(http);
        var gateway = new TelegramStarsBillingGateway(client, Options.Create(new TelegramStarsOptions { BotToken = "123:synthetic" }), TimeProvider.System);
        DateTime start = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
        Result<BillingCancellationSnapshotModel> result = await gateway.CancelSubscriptionAsync(new BillingCancellationRequestModel("invoice_owned", "123", "first_charge", start, start.AddDays(30)));
        ResultAssert.Success(result);
        using var json = JsonDocument.Parse(http.LastRequestBody!);
        Assert.Multiple(() => {
            Assert.True(result.Value.CancelAtPeriodEnd);
            Assert.Equal(start.AddDays(30), result.Value.CurrentPeriodEndUtc);
            Assert.Equal("first_charge", json.RootElement.GetProperty("telegram_payment_charge_id").GetString());
            Assert.True(json.RootElement.GetProperty("is_canceled").GetBoolean());
        });
    }
}
