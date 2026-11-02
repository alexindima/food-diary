using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingCancellationBoundaryTests {
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CancellationIdentity_CopiesAndFormatsWithoutChangingTheOriginal() {
        var value = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var original = new BillingCancellationId(Guid.Empty);
        BillingCancellationId copy = original with { Value = value };
        Guid roundTrip = copy.Value;

        Assert.Multiple(
            () => Assert.Equal(value, roundTrip),
            () => Assert.Equal((BillingCancellationId)value, copy),
            () => Assert.Equal(Guid.Empty, original.Value),
            () => Assert.Equal(value.ToString(), copy.ToString()),
            () => Assert.NotEqual(Guid.Empty, BillingCancellationId.New().Value));
    }

    [Fact]
    public void UnconfirmedSubscription_CannotCreateCancellationHistory() {
        var subscription = BillingSubscription.CreatePending(UserId.New(), BillingProviderNames.Paddle, "ctm_owned", "pri_monthly", "monthly");

        Assert.Equal("subscription", Assert.Throws<ArgumentException>(() => BillingCancellation.Create(subscription, "fooddiary", Now, Now.AddDays(30))).ParamName);
    }

    [Fact]
    public void ConfirmedHistory_RequiresAKnownSourceAndPreservesRequestContext() {
        BillingSubscription subscription = Scheduled();
        Assert.Equal("source", Assert.Throws<ArgumentException>(() => BillingCancellation.Create(subscription, "unknown", Now, Now.AddDays(30))).ParamName);

        var cancellation = BillingCancellation.Create(subscription, "paddle", Now, Now.AddDays(30));

        Assert.Multiple(
            () => Assert.Equal("monthly", cancellation.Plan),
            () => Assert.Equal("paddle", cancellation.Source),
            () => Assert.Equal(Now, cancellation.RequestedAtUtc));
    }

    [Fact]
    public void Synchronize_RejectsAnotherSubscriptionsOwnerWithoutChangingHistory() {
        var cancellation = BillingCancellation.Create(Scheduled(), "fooddiary", Now, Now.AddDays(30));

        Assert.Throws<ArgumentException>(() => cancellation.Synchronize(Scheduled(), Now));

        Assert.Multiple(
            () => Assert.Equal("scheduled", cancellation.Outcome),
            () => Assert.Equal(Now.AddDays(30), cancellation.EffectiveAtUtc),
            () => Assert.Null(cancellation.ModifiedOnUtc));
    }

    [Fact]
    public void MarkEmailHandled_IsIdempotentAndPreservesTheOriginalHandlingInstant() {
        var cancellation = BillingCancellation.Create(Scheduled(), "fooddiary", Now, Now.AddDays(30));

        cancellation.MarkConfirmationEmailHandled(Now);
        cancellation.MarkConfirmationEmailHandled(Now.AddDays(1));

        Assert.Multiple(
            () => Assert.Equal(Now, cancellation.ConfirmationEmailHandledAtUtc),
            () => Assert.Equal(Now, cancellation.ModifiedOnUtc));
    }

    private static BillingSubscription Scheduled() {
        var subscription = BillingSubscription.CreatePending(UserId.New(), BillingProviderNames.Paddle, "ctm_owned", "pri_monthly", "monthly");
        subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_owned", externalPaymentMethodId: null,
            "pri_monthly", "monthly", "active", Now, Now.AddDays(30), cancelAtPeriodEnd: true,
            canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null, "evt_cancel", Now);
        return subscription;
    }
}
