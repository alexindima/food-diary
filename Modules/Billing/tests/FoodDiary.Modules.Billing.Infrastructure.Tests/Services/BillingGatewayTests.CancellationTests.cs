using System.Text.Json;
using FoodDiary.Testing.Assertions;
using System.Net;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using FoodDiary.Results;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Services;

public partial class BillingGatewayTests {
    [Theory]
    [InlineData("active", "next_billing_period", true)]
    [InlineData("trialing", "next_billing_period", true)]
    [InlineData("paused", "immediately", false)]
    [InlineData("past_due", "immediately", false)]
    public async Task PaddleCancellation_UsesProviderStateAndConfirmsEffectiveDate(string status, string effectiveFrom, bool scheduled) {
        var http = new RecordingHttpMessageHandler(CancellationResponse(status),
            CancellationResponse(scheduled ? status : "canceled", scheduled));
        PaddleBillingGateway gateway = CreateCancellationGateway(http);

        Result<BillingCancellationSnapshotModel> result = await gateway.CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_owned"));

        ResultAssert.Success(result);
        Assert.Multiple(() => {
            Assert.Equal(scheduled, result.Value.CancelAtPeriodEnd);
            Assert.Equal(new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc), result.Value.EffectiveAtUtc);
            Assert.Equal(HttpMethod.Get, http.Requests[0].Method);
            Assert.Equal(HttpMethod.Post, http.Requests[1].Method);
            Assert.Contains($"\"effective_from\":\"{effectiveFrom}\"", http.RequestBodies[1], StringComparison.Ordinal);
            Assert.EndsWith("/subscriptions/sub_owned/cancel", http.Requests[1].RequestUri!.AbsolutePath, StringComparison.Ordinal);
        });
    }

    [Theory]
    [InlineData("active", true)]
    [InlineData("canceled", false)]
    public async Task PaddleCancellation_AlreadyConfirmed_DoesNotPostAgain(string status, bool scheduled) {
        var http = new RecordingHttpMessageHandler(CancellationResponse(status, scheduled));
        Result<BillingCancellationSnapshotModel> result = await CreateCancellationGateway(http)
            .CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_owned"));
        ResultAssert.Success(result);
        Assert.Single(http.Requests);
        Assert.Equal(HttpMethod.Get, http.LastRequest!.Method);
    }

    [Fact]
    public async Task PaddleCancellation_CustomerMismatch_NeverPosts() {
        var http = new RecordingHttpMessageHandler(CancellationResponse("active"));
        Result<BillingCancellationSnapshotModel> result = await CreateCancellationGateway(http)
            .CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_someone_else"));
        ResultAssert.Failure(result);
        Assert.Single(http.Requests);
    }

    [Fact]
    public async Task PaddleCancellation_LostResponse_RecoversFromFreshGet() {
        var http = new ScriptedHttpMessageHandler(CancellationResponse("active"),
            new HttpRequestException("Response lost after acceptance"), CancellationResponse("active", scheduled: true));
        Result<BillingCancellationSnapshotModel> result = await CreateCancellationGateway(http)
            .CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_owned"));
        ResultAssert.Success(result);
        Assert.Equal(3, http.RequestCount);
    }

    [Fact]
    public async Task PaddleCancellation_NotConfirmed_DoesNotClaimSuccess() {
        var http = new RecordingHttpMessageHandler(CancellationResponse("active"),
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), CancellationResponse("active"));
        Result<BillingCancellationSnapshotModel> result = await CreateCancellationGateway(http)
            .CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_owned"));
        ResultAssert.Failure(result);
        Assert.Equal(3, http.Requests.Count);
    }

    [Fact]
    public async Task PaddleCancellation_Rfc3339Offsets_NormalizesProviderInstants() {
        var http = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent("""
                {"data":{"id":"sub_owned","customer_id":"ctm_owned","status":"active",
                "current_billing_period":{"starts_at":"2026-11-01T14:00:00+04:00","ends_at":"2026-12-01T14:00:00+04:00"},
                "scheduled_change":{"action":"cancel","effective_at":"2026-12-01T14:00:00+04:00"},
                "updated_at":"2026-11-01T14:01:00+04:00"}}
                """),
        });
        Result<BillingCancellationSnapshotModel> result = await CreateCancellationGateway(http)
            .CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_owned"));
        ResultAssert.Success(result);
        Assert.True(result.Value.ObservedAtUtc.Kind == DateTimeKind.Utc && result.Value.EffectiveAtUtc.Kind == DateTimeKind.Utc,
            "Paddle cancellation observations must be normalized to UTC.");
        Assert.Equal(new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc), result.Value.EffectiveAtUtc);
    }

    private static PaddleBillingGateway CreateCancellationGateway(HttpMessageHandler handler) =>
        new(new HttpClient(handler), Options.Create(new PaddleOptions {
            ApiKey = "synthetic-cancellation-test-key",
            ApiBaseUrl = "https://sandbox-api.paddle.test",
        }));

    private static HttpResponseMessage CancellationResponse(string status, bool scheduled = false) =>
        new(HttpStatusCode.OK) { Content = JsonContent(JsonSerializer.Serialize(new {
            data = new {
                id = "sub_owned", customer_id = "ctm_owned", status,
                current_billing_period = new { starts_at = "2026-11-01T10:00:00Z", ends_at = "2026-12-01T10:00:00Z" },
                scheduled_change = scheduled ? new { action = "cancel", effective_at = "2026-12-01T10:00:00Z" } : null,
                canceled_at = string.Equals(status, "canceled", StringComparison.Ordinal) ? "2026-12-01T10:00:00Z" : null,
                updated_at = "2026-11-01T10:01:00Z",
            },
        })), };
}
