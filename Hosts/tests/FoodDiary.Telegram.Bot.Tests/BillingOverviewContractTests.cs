using System.Text.Json;
using FoodDiary.Telegram.Bot.Api.Generated.Model;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingOverviewContractTests {
    [Fact]
    public void BillingOverview_PreservesTheCompleteWireContractDuringRoundTrip() {
        const string json = """
            {
              "availableProviders": ["paddle"],
              "canStartPremiumTrial": false,
              "cancelAtPeriodEnd": false,
              "cancellationAvailable": true,
              "currentPeriodEndUtc": "2026-12-01T00:00:00Z",
              "currentPeriodStartUtc": "2026-11-01T00:00:00Z",
              "isPremium": true,
              "manageBillingAvailable": true,
              "nextBillingAttemptUtc": null,
              "paddleClientToken": "synthetic-client-token",
              "paddleMonthlyPriceId": "pri_monthly",
              "paddleYearlyPriceId": "pri_yearly",
              "plan": "monthly",
              "premiumTrialActive": false,
              "premiumTrialEndUtc": "2026-10-08T00:00:00Z",
              "premiumTrialStartUtc": "2026-10-01T00:00:00Z",
              "premiumTrialUsed": true,
              "provider": "paddle",
              "renewalEnabled": true,
              "subscriptionProvider": "paddle",
              "subscriptionStatus": "active"
            }
            """;
        BillingOverviewHttpResponse response = Assert.IsType<BillingOverviewHttpResponse>(
            JsonSerializer.Deserialize<BillingOverviewHttpResponse>(json));
        using var expected = JsonDocument.Parse(json);

        JsonElement actual = JsonSerializer.SerializeToElement(response);

        Assert.True(JsonElement.DeepEquals(expected.RootElement, actual), actual.GetRawText());
    }
}
