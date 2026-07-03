using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Billing.Commands.CreateCheckoutSession;
using FoodDiary.Application.Billing.Commands.CreatePortalSession;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Billing.Queries.GetBillingOverview;
using FoodDiary.Presentation.Api.Features.Billing.Mappings;
using FoodDiary.Presentation.Api.Features.Billing.Requests;
using FoodDiary.Presentation.Api.Features.Billing.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingHttpMappingsTests {
    [Fact]
    public void CreateCheckoutSessionHttpRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new CreateCheckoutSessionHttpRequest("premium", "stripe");

        CreateCheckoutSessionCommand command = request.ToCommand(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal("premium", command.Plan),
            () => Assert.Equal("stripe", command.Provider));
    }

    [Fact]
    public void UserId_ToBillingCommandsAndQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        CreatePortalSessionCommand portalCommand = userId.ToPortalSessionCommand();
        var trialCommand = userId.ToStartPremiumTrialCommand();
        GetBillingOverviewQuery overviewQuery = userId.ToBillingOverviewQuery();

        Assert.Multiple(
            () => Assert.Equal(userId, portalCommand.UserId),
            () => Assert.Equal(userId, trialCommand.UserId),
            () => Assert.Equal(userId, overviewQuery.UserId));
    }

    [Fact]
    public void BillingOverviewModel_ToHttpResponse_MapsAllFields() {
        DateTime periodStart = DateTime.UtcNow.AddDays(-1);
        DateTime periodEnd = DateTime.UtcNow.AddDays(29);
        DateTime nextAttempt = DateTime.UtcNow.AddDays(30);
        DateTime trialStart = DateTime.UtcNow.AddDays(-3);
        DateTime trialEnd = DateTime.UtcNow.AddDays(4);
        var model = new BillingOverviewModel(
            IsPremium: true,
            SubscriptionStatus: "active",
            Plan: "premium",
            SubscriptionProvider: "stripe",
            CurrentPeriodStartUtc: periodStart,
            CurrentPeriodEndUtc: periodEnd,
            NextBillingAttemptUtc: nextAttempt,
            CancelAtPeriodEnd: true,
            RenewalEnabled: false,
            ManageBillingAvailable: true,
            PremiumTrialStartUtc: trialStart,
            PremiumTrialEndUtc: trialEnd,
            PremiumTrialActive: true,
            PremiumTrialUsed: true,
            CanStartPremiumTrial: false,
            Provider: "paddle",
            PaddleClientToken: "client-token",
            AvailableProviders: ["paddle", "stripe"]);

        BillingOverviewHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.True(response.IsPremium),
            () => Assert.Equal("active", response.SubscriptionStatus),
            () => Assert.Equal("premium", response.Plan),
            () => Assert.Equal("stripe", response.SubscriptionProvider),
            () => Assert.Equal(periodStart, response.CurrentPeriodStartUtc),
            () => Assert.Equal(periodEnd, response.CurrentPeriodEndUtc),
            () => Assert.Equal(nextAttempt, response.NextBillingAttemptUtc),
            () => Assert.True(response.CancelAtPeriodEnd),
            () => Assert.False(response.RenewalEnabled),
            () => Assert.True(response.ManageBillingAvailable),
            () => Assert.Equal(trialStart, response.PremiumTrialStartUtc),
            () => Assert.Equal(trialEnd, response.PremiumTrialEndUtc),
            () => Assert.True(response.PremiumTrialActive),
            () => Assert.True(response.PremiumTrialUsed),
            () => Assert.False(response.CanStartPremiumTrial),
            () => Assert.Equal("paddle", response.Provider),
            () => Assert.Equal("client-token", response.PaddleClientToken),
            () => Assert.Equal(["paddle", "stripe"], response.AvailableProviders));
    }

    [Fact]
    public void BillingSessionModels_ToHttpResponse_MapAllFields() {
        var checkout = new BillingCheckoutSessionModel(
            "session-1",
            "https://checkout.example",
            "customer-1",
            "price-1",
            "premium");
        var portal = new BillingPortalSessionModel("https://portal.example");

        CheckoutSessionHttpResponse checkoutResponse = checkout.ToHttpResponse();
        PortalSessionHttpResponse portalResponse = portal.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal("session-1", checkoutResponse.SessionId),
            () => Assert.Equal("https://checkout.example", checkoutResponse.Url),
            () => Assert.Equal("premium", checkoutResponse.Plan),
            () => Assert.Equal("https://portal.example", portalResponse.Url));
    }
}
