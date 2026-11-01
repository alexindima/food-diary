using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingCancellationTests {
    private static readonly DateTime Now = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(null, null)]
    [InlineData("too_expensive", "  Price  ")]
    [InlineData(null, "Comment only")]
    public void OptionalFeedback_DoesNotChangeCancellation(string? reason, string? comment) {
        BillingSubscription subscription = CreateScheduledSubscription();
        var cancellation = BillingCancellation.Create(subscription, "fooddiary", Now, Now.AddDays(30));
        cancellation.SubmitFeedback(reason, comment, Now);
        Assert.Multiple(() => {
            Assert.Equal("scheduled", cancellation.Outcome);
            Assert.Equal(reason, cancellation.ReasonCode);
            Assert.Equal(comment?.Trim(), cancellation.Comment);
            Assert.Equal(reason is null && comment is null ? null : (DateTime?)Now, cancellation.FeedbackSubmittedAtUtc);
            Assert.Equal(Now.AddDays(30), cancellation.EffectiveAtUtc);
        });
    }

    [Fact]
    public void InvalidFeedback_IsAtomic() {
        var cancellation = BillingCancellation.Create(CreateScheduledSubscription(), "fooddiary", Now, Now.AddDays(30));
        cancellation.SubmitFeedback("other", "original", Now);
        Assert.Throws<ArgumentException>(() => cancellation.SubmitFeedback("unknown", "replacement", Now));
        Assert.Throws<ArgumentException>(() => cancellation.SubmitFeedback("too_expensive", new string('a', 2001), Now));
        Assert.Equal("other", cancellation.ReasonCode);
        Assert.Equal("original", cancellation.Comment);
    }

    [Fact]
    public void ProviderCompletionAndResumption_PreserveOriginalFeedback() {
        BillingSubscription subscription = CreateScheduledSubscription();
        var cancellation = BillingCancellation.Create(subscription, "fooddiary", Now, Now.AddDays(30));
        cancellation.SubmitFeedback("missing_features", "Feature request", Now);
        subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_owned", externalPaymentMethodId: null,
            "pri_monthly", "monthly", "canceled", Now, Now.AddDays(30), cancelAtPeriodEnd: false,
            Now.AddDays(30), trialStartUtc: null, trialEndUtc: null, "evt_end", Now.AddDays(30));
        cancellation.Synchronize(subscription, Now.AddDays(30));
        Assert.Equal("canceled", cancellation.Outcome);
        Assert.Equal("missing_features", cancellation.ReasonCode);
        Assert.Equal("Feature request", cancellation.Comment);
    }

    private static BillingSubscription CreateScheduledSubscription() {
        var subscription = BillingSubscription.CreatePending(UserId.New(), BillingProviderNames.Paddle, "ctm_owned", "pri_monthly", "monthly");
        subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_owned", externalPaymentMethodId: null,
            "pri_monthly", "monthly", "active", Now, Now.AddDays(30), cancelAtPeriodEnd: true,
            canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null, "evt_cancel", Now);
        return subscription;
    }
}
