using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Presentation.Api.Features.Billing.Mappings;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingHttpMappingsTests {
    [Fact]
    public void BillingOverviewModel_ToHttpResponse_MapsAllFields() {
        var periodStart = DateTime.UtcNow.AddDays(-1);
        var periodEnd = DateTime.UtcNow.AddDays(29);
        var nextAttempt = DateTime.UtcNow.AddDays(30);
        var trialStart = DateTime.UtcNow.AddDays(-3);
        var trialEnd = DateTime.UtcNow.AddDays(4);
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

        var response = model.ToHttpResponse();

        Assert.True(response.IsPremium);
        Assert.Equal("active", response.SubscriptionStatus);
        Assert.Equal("premium", response.Plan);
        Assert.Equal("stripe", response.SubscriptionProvider);
        Assert.Equal(periodStart, response.CurrentPeriodStartUtc);
        Assert.Equal(periodEnd, response.CurrentPeriodEndUtc);
        Assert.Equal(nextAttempt, response.NextBillingAttemptUtc);
        Assert.True(response.CancelAtPeriodEnd);
        Assert.False(response.RenewalEnabled);
        Assert.True(response.ManageBillingAvailable);
        Assert.Equal(trialStart, response.PremiumTrialStartUtc);
        Assert.Equal(trialEnd, response.PremiumTrialEndUtc);
        Assert.True(response.PremiumTrialActive);
        Assert.True(response.PremiumTrialUsed);
        Assert.False(response.CanStartPremiumTrial);
        Assert.Equal("paddle", response.Provider);
        Assert.Equal("client-token", response.PaddleClientToken);
        Assert.Equal(["paddle", "stripe"], response.AvailableProviders);
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

        var checkoutResponse = checkout.ToHttpResponse();
        var portalResponse = portal.ToHttpResponse();

        Assert.Equal("session-1", checkoutResponse.SessionId);
        Assert.Equal("https://checkout.example", checkoutResponse.Url);
        Assert.Equal("premium", checkoutResponse.Plan);
        Assert.Equal("https://portal.example", portalResponse.Url);
    }
}
