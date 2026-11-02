using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Fact]
    public async Task NewerSignedCancellation_WinsOverTheOlderHttpConfirmation() {
        var fixture = new CancellationFixture();
        fixture.Gateway.CancelSubscriptionAsync(Arg.Any<BillingCancellationRequestModel>(), Arg.Any<CancellationToken>()).Returns(_ => {
            fixture.Subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_owned", externalPaymentMethodId: null,
                "price_monthly", "monthly", "active", Now, Now.AddDays(40), cancelAtPeriodEnd: true, canceledAtUtc: null,
                trialStartUtc: null, trialEndUtc: null, "evt_signed_cancellation", Now.AddMinutes(2), webhookOccurredAtUtc: Now.AddMinutes(2));
            return Result.Success(fixture.Snapshot);
        });

        Result<BillingCancellationModel> result = await fixture.CancelAsync();

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.True(fixture.Subscription.CancelAtPeriodEnd),
            () => Assert.Equal("evt_signed_cancellation", fixture.Subscription.LastWebhookEventId),
            () => Assert.Equal(Now.AddDays(40), fixture.Subscription.CurrentPeriodEndUtc),
            () => Assert.Equal(Now.AddDays(40), Assert.Single(fixture.History).EffectiveAtUtc));
    }
}
