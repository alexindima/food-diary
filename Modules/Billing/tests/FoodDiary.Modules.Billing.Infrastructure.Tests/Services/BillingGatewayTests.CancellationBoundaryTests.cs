using System.Net;
using System.Text.Json.Nodes;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Tests.Services;

public partial class BillingGatewayTests {
    [Fact]
    public async Task PaddleCancellation_UnconfiguredGatewayDoesNotSendHttp() {
        var http = new RecordingHttpMessageHandler(CancellationResponse("active"));
        var gateway = new PaddleBillingGateway(new HttpClient(http), Options.Create(new PaddleOptions()));

        Result<BillingCancellationSnapshotModel> result = await gateway.CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_owned"));

        Assert.Equal(BillingErrors.ProviderNotConfigured(BillingProviderNames.Paddle), ResultAssert.Failure(result));
        Assert.Empty(http.Requests);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("incomplete")]
    public async Task PaddleCancellation_UnsupportedSubscriptionStateDoesNotPost(string status) {
        var http = new RecordingHttpMessageHandler(CancellationResponse(status));

        Result<BillingCancellationSnapshotModel> result = await CreateCancellationGateway(http)
            .CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_owned"));

        Assert.Equal(BillingErrors.CancellationUnavailable, ResultAssert.Failure(result));
        Assert.Equal(HttpMethod.Get, Assert.Single(http.Requests).Method);
    }

    [Fact]
    public async Task PaddleCancellation_InitialProviderFailureIsReturnedWithoutPosting() {
        var http = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        Result<BillingCancellationSnapshotModel> result = await CreateCancellationGateway(http)
            .CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_owned"));

        ResultAssert.Failure(result);
        Assert.Equal(HttpMethod.Get, Assert.Single(http.Requests).Method);
    }

    [Fact]
    public async Task PaddleCancellation_FailedInitialConnectionReturnsAnActionableProviderError() {
        var http = new ScriptedHttpMessageHandler(new HttpRequestException("Synthetic unavailable provider"));

        Result<BillingCancellationSnapshotModel> result = await CreateCancellationGateway(http)
            .CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_owned"));

        Assert.Contains("Refresh and try again", ResultAssert.Failure(result).Message, StringComparison.Ordinal);
        Assert.Equal(1, http.RequestCount);
    }

    [Theory]
    [InlineData("missing-update")]
    [InlineData("missing-effective")]
    [InlineData("different-period")]
    [InlineData("inverted-period")]
    public async Task PaddleCancellation_InvalidConfirmationDatesCannotClaimSuccess(string fault) {
        HttpResponseMessage response = CancellationResponse("active", scheduled: true);
        JsonNode payload = JsonNode.Parse(await response.Content!.ReadAsStringAsync())!;
        JsonNode data = payload["data"]!;
        switch (fault) {
            case "missing-update": data["updated_at"] = null; break;
            case "missing-effective": data["scheduled_change"]!["effective_at"] = null; break;
            case "different-period": data["scheduled_change"]!["effective_at"] = "2026-12-02T10:00:00Z"; break;
            case "inverted-period": data["current_billing_period"]!["starts_at"] = "2027-01-01T10:00:00Z"; break;
        }
        response.Content = JsonContent(payload.ToJsonString());
        var http = new RecordingHttpMessageHandler(response);

        Result<BillingCancellationSnapshotModel> result = await CreateCancellationGateway(http)
            .CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_owned"));

        Assert.Contains("incomplete cancellation dates", ResultAssert.Failure(result).Message, StringComparison.Ordinal);
        Assert.Single(http.Requests);
    }

    [Fact]
    public async Task ProviderWithoutCancellationSupport_FailsClosedThroughTheGatewayContract() {
        IBillingProviderGateway gateway = new StripeBillingGateway(Options.Create(new StripeOptions()));

        Result<BillingCancellationSnapshotModel> result = await gateway.CancelSubscriptionAsync(new BillingCancellationRequestModel("sub_owned", "ctm_owned"));

        ResultAssert.Failure(result);
        Assert.Equal(BillingErrors.CancellationUnavailable, result.Error);
    }
}
