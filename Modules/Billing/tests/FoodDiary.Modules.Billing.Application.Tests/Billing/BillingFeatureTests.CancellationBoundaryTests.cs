using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.SubmitCancellationFeedback;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Modules.Billing.Application.Queries.GetBillingOverview;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.CheckUserAccess;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Fact]
    public async Task Cancellation_DeniedOwnerCannotContactTheProvider() {
        var fixture = new CancellationFixture();
        var denied = new Error("Billing.Denied", "Denied", ErrorKind.Forbidden);
        fixture.Sender.Send(Arg.Any<CheckUserAccessQuery>(), Arg.Any<CancellationToken>()).Returns(denied);

        Assert.Equal(denied, ResultAssert.Failure(await fixture.CancelAsync()));

        await fixture.Gateway.DidNotReceiveWithAnyArgs().CancelSubscriptionAsync(default!, default);
        Assert.Empty(fixture.History);
    }

    [Fact]
    public async Task Cancellation_MissingGatewayDoesNotClaimSuccess() {
        var fixture = new CancellationFixture();
        fixture.Gateways.GetProviderOrDefault(BillingProviderNames.Paddle).Returns((IBillingProviderGateway?)null);

        Assert.Equal(BillingErrors.CancellationUnavailable, ResultAssert.Failure(await fixture.CancelAsync()));

        Assert.Empty(fixture.History);
    }

    [Fact]
    public async Task Cancellation_PrincipalDisappearsDuringProviderHttp_DoesNotApplyTheResponse() {
        var fixture = new CancellationFixture();
        var error = new Error("Users.NotFound", "Missing user", ErrorKind.NotFound);
        fixture.Sender.Send(Arg.Any<GetUserBillingProfileQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<UserBillingProfileModel>(error));

        Assert.Equal(error, ResultAssert.Failure(await fixture.CancelAsync()));

        Assert.False(fixture.Subscription.CancelAtPeriodEnd);
        Assert.Empty(fixture.History);
    }

    [Fact]
    public async Task Cancellation_ConcurrentCheckoutIsNotOverwrittenByTheProviderResponse() {
        var fixture = new CancellationFixture();
        fixture.Gateway.CancelSubscriptionAsync(Arg.Any<BillingCancellationRequestModel>(), Arg.Any<CancellationToken>()).Returns(_ => {
            fixture.Subscription.UpdateCheckoutContext(BillingProviderNames.Paddle, "ctm_owned", "price_new", "yearly");
            return Result.Success(fixture.Snapshot);
        });

        Assert.Equal(BillingErrors.CancellationUnavailable, ResultAssert.Failure(await fixture.CancelAsync()));

        Assert.Multiple(
            () => Assert.Equal(BillingSubscription.PendingCheckoutStatus, fixture.Subscription.Status),
            () => Assert.Equal("yearly", fixture.Subscription.Plan),
            () => Assert.Empty(fixture.History));
    }

    [Fact]
    public async Task Cancellation_UnconfirmedProviderSnapshotCannotCreateHistory() {
        var fixture = new CancellationFixture();
        fixture.Gateway.CancelSubscriptionAsync(Arg.Any<BillingCancellationRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(fixture.Snapshot with { CancelAtPeriodEnd = false }));

        Assert.Equal(BillingErrors.CancellationUnavailable, ResultAssert.Failure(await fixture.CancelAsync()));

        Assert.Empty(fixture.History);
    }

    [Fact]
    public async Task Cancellation_OverviewFailureDoesNotUndoTheConfirmedCancellation() {
        var fixture = new CancellationFixture();
        var error = new Error("Billing.OverviewUnavailable", "Synthetic read failure", ErrorKind.Validation);
        fixture.Sender.Send(Arg.Any<GetBillingOverviewQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<BillingOverviewModel>(error));

        Assert.Equal(error, ResultAssert.Failure(await fixture.CancelAsync()));

        Assert.True(fixture.Subscription.CancelAtPeriodEnd);
        Assert.Equal("scheduled", Assert.Single(fixture.History).Outcome);
    }

    [Fact]
    public async Task Cancellation_ResumptionUpdatesExistingHistoryWithoutCreatingAnotherCancellation() {
        var fixture = new CancellationFixture();
        ResultAssert.Success(await fixture.CancelAsync());
        BillingCancellation history = Assert.Single(fixture.History);
        fixture.Subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_owned", externalPaymentMethodId: null, "price_monthly", "monthly",
            "active", Now, Now.AddDays(30), cancelAtPeriodEnd: false, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null,
            "evt_resumed", Now.AddMinutes(2), webhookOccurredAtUtc: Now.AddMinutes(2));

        BillingCancellation? synchronized = await new BillingCancellationRecorder(fixture.Cancellations, new FixedDateTimeProvider(Now.AddMinutes(2)))
            .SynchronizeAsync(fixture.Subscription, "paddle", CancellationToken.None);

        Assert.Null(synchronized);
        Assert.Same(history, Assert.Single(fixture.History));
        Assert.Equal("cancellation_removed", Assert.Single(fixture.History).Outcome);
    }

    [Fact]
    public async Task CancellationFeedback_DeniedOwnerCannotReadOrChangePrivateHistory() {
        var fixture = new CancellationFixture();
        var denied = new Error("Billing.Denied", "Denied", ErrorKind.Forbidden);
        fixture.Sender.Send(Arg.Any<CheckUserAccessQuery>(), Arg.Any<CancellationToken>()).Returns(denied);
        var handler = new SubmitCancellationFeedbackCommandHandler(fixture.Sender, fixture.Cancellations, new FixedDateTimeProvider(Now), new NoOpBillingTransactionRunner());

        Result result = await handler.Handle(new SubmitCancellationFeedbackCommand(fixture.UserId.Value, BillingCancellationId.New(), ReasonCode: null, Comment: null), CancellationToken.None);

        Assert.Equal(denied, ResultAssert.Failure(result));
        await fixture.Cancellations.DidNotReceiveWithAnyArgs().GetOwnedAsync(default, default, default);
    }
}
