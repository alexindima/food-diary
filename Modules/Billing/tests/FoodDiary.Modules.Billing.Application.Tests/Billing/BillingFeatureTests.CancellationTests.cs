using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.CancelSubscription;
using FoodDiary.Modules.Billing.Application.Commands.SubmitCancellationFeedback;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Modules.Billing.Application.Queries.GetBillingOverview;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Queries.CheckUserAccess;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Fact]
    public async Task Cancellation_EmptySurvey_PreservesPaidAccessAndOneHistoryRecordAcrossRetries() {
        var fixture = new CancellationFixture();
        Result<BillingOverviewModel> initial = await fixture.Sender.Send(new GetBillingOverviewQuery(fixture.UserId.Value), CancellationToken.None);
        ResultAssert.Success(initial);
        Assert.True(initial.Value.CancellationAvailable);
        Result<BillingCancellationModel> first = await fixture.CancelAsync();
        Result<BillingCancellationModel> retry = await fixture.CancelAsync();

        ResultAssert.Success(first);
        ResultAssert.Success(retry);
        BillingCancellation history = Assert.Single(fixture.History);
        Assert.Multiple(() => {
            Assert.Equal(first.Value.CancellationId, retry.Value.CancellationId);
            Assert.True(fixture.Subscription.CancelAtPeriodEnd);
            Assert.Equal("active", fixture.Subscription.Status);
            Assert.Equal(Now.AddDays(30), fixture.Subscription.CurrentPeriodEndUtc);
            Assert.Null(fixture.Subscription.NextBillingAttemptUtc);
            Assert.True(first.Value.Overview.IsPremium);
            Assert.False(first.Value.Overview.RenewalEnabled);
            Assert.False(first.Value.Overview.CancellationAvailable);
            Assert.Null(history.ReasonCode);
            Assert.Null(history.FeedbackSubmittedAtUtc);
        });
    }

    [Fact]
    public async Task Cancellation_FailedOrInvalidSurvey_DoesNotUndoConfirmedCancellation() {
        var fixture = new CancellationFixture();
        Result<BillingCancellationModel> canceled = await fixture.CancelAsync();
        ResultAssert.Success(canceled);
        var handler = new SubmitCancellationFeedbackCommandHandler(fixture.Sender, fixture.Cancellations, new FixedDateTimeProvider(Now), new NoOpBillingTransactionRunner());
        Result invalid = await handler.Handle(new SubmitCancellationFeedbackCommand(fixture.UserId.Value,
            new BillingCancellationId(canceled.Value.CancellationId), "not_a_reason", new string('a', 2001)), CancellationToken.None);
        ResultAssert.Failure(invalid);
        fixture.Cancellations.UpdateAsync(Arg.Any<BillingCancellation>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Synthetic feedback persistence failure")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new SubmitCancellationFeedbackCommand(
            fixture.UserId.Value, new BillingCancellationId(canceled.Value.CancellationId), "too_expensive", "Price"), CancellationToken.None));
        Assert.True(fixture.Subscription.CancelAtPeriodEnd);
        Assert.Null(fixture.Subscription.NextBillingAttemptUtc);
    }

    [Fact]
    public async Task Cancellation_AnotherUsersFeedback_ReturnsNotFound() {
        var fixture = new CancellationFixture();
        await fixture.CancelAsync();
        var handler = new SubmitCancellationFeedbackCommandHandler(fixture.Sender, fixture.Cancellations, new FixedDateTimeProvider(Now), new NoOpBillingTransactionRunner());
        Result result = await handler.Handle(new SubmitCancellationFeedbackCommand(Guid.NewGuid(), fixture.History[0].TypedId,
            "other", "Private"), CancellationToken.None);
        ResultAssert.Failure(result);
        Assert.Equal("Billing.CancellationNotFound", result.Error.Code);
        Assert.Null(fixture.History[0].Comment);
    }

    [Fact]
    public async Task Cancellation_PendingCheckoutWithHistoricalSubscriptionId_IsNotOverwritten() {
        var fixture = new CancellationFixture();
        fixture.Subscription.UpdateCheckoutContext(BillingProviderNames.Paddle, "ctm_owned", "price_monthly", "monthly");
        Assert.Equal("sub_owned", fixture.Subscription.ExternalSubscriptionId);
        Result<BillingCancellationModel> result = await fixture.CancelAsync();
        Assert.True(result.IsFailure, "Pending checkout must not be overwritten by cancellation of a historical subscription.");
        Assert.Equal(BillingSubscription.PendingCheckoutStatus, fixture.Subscription.Status);
        Assert.Empty(fixture.History);
        await fixture.Gateway.DidNotReceive().CancelSubscriptionAsync(Arg.Any<BillingCancellationRequestModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancellation_ProviderFailure_DoesNotChangeSubscriptionOrRecordHistory() {
        var fixture = new CancellationFixture();
        fixture.Gateway.CancelSubscriptionAsync(Arg.Any<BillingCancellationRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<BillingCancellationSnapshotModel>(BillingErrors.ProviderOperationFailed(BillingProviderNames.Paddle, "unavailable")));
        Result<BillingCancellationModel> result = await fixture.CancelAsync();
        ResultAssert.Failure(result);
        Assert.False(fixture.Subscription.CancelAtPeriodEnd);
        Assert.Empty(fixture.History);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Cancellation_NewerResumptionDuringProviderHttp_IsNotOverwritten(int minutes) {
        var fixture = new CancellationFixture();
        fixture.Gateway.CancelSubscriptionAsync(Arg.Any<BillingCancellationRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                fixture.Subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_owned", externalPaymentMethodId: null, "price_monthly", "monthly",
                    "active", Now, Now.AddDays(30), cancelAtPeriodEnd: false, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null, "evt_resumed", Now.AddMinutes(minutes), webhookOccurredAtUtc: Now.AddMinutes(minutes));
                return Result.Success(fixture.Snapshot);
            });
        Result<BillingCancellationModel> result = await fixture.CancelAsync();
        Assert.True(result.IsFailure, "A concurrent resumption must not be overwritten by an equal-time cancellation response.");
        Assert.False(fixture.Subscription.CancelAtPeriodEnd);
        Assert.Equal("evt_resumed", fixture.Subscription.LastWebhookEventId);
        Assert.Empty(fixture.History);
    }

    [ExcludeFromCodeCoverage]
    private sealed class CancellationFixture {
        public UserId UserId { get; } = UserId.New();
        public ISender Sender { get; } = Substitute.For<ISender>();
        public IBillingProviderGateway Gateway { get; } = Substitute.For<IBillingProviderGateway>();
        public IBillingCancellationRepository Cancellations { get; } = Substitute.For<IBillingCancellationRepository>();
        public List<BillingCancellation> History { get; } = [];
        public BillingSubscription Subscription { get; }
        public BillingCancellationSnapshotModel Snapshot { get; } = new("active", Now, Now.AddDays(30), CancelAtPeriodEnd: true, CanceledAtUtc: null, Now.AddDays(30), Now.AddMinutes(1));
        private readonly CancelSubscriptionCommandHandler _handler;

        public CancellationFixture() {
            var time = new FixedDateTimeProvider(Now);
            Subscription = BillingSubscription.CreatePending(UserId, BillingProviderNames.Paddle, "ctm_owned", "price_monthly", "monthly");
            Subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_owned", externalPaymentMethodId: null, "price_monthly", "monthly", "active",
                Now, Now.AddDays(30), cancelAtPeriodEnd: false, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null, "evt_active", Now, webhookOccurredAtUtc: Now);
            var subscriptions = new InMemoryBillingSubscriptionRepository(Subscription);
            Sender.Send(Arg.Any<CheckUserAccessQuery>(), Arg.Any<CancellationToken>()).Returns((Error?)null);
            Sender.Send(Arg.Any<GetUserBillingProfileQuery>(), Arg.Any<CancellationToken>()).Returns(
                Result.Success(new UserBillingProfileModel(UserId, "synthetic@example.test", IsActive: true, IsDeleted: false, HasPaidPremium: true, PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, IsEmailConfirmed: true)));
            IBillingPublicConfigProvider config = Substitute.For<IBillingPublicConfigProvider>();
            config.GetPublicConfig().Returns(new BillingPublicConfigModel(BillingProviderNames.Paddle, PaddleClientToken: null, [BillingProviderNames.Paddle]));
            var overview = new GetBillingOverviewQueryHandler(Sender, subscriptions, config, time);
            Sender.Send(Arg.Any<GetBillingOverviewQuery>(), Arg.Any<CancellationToken>())
                .Returns(call => overview.Handle(call.Arg<GetBillingOverviewQuery>(), call.Arg<CancellationToken>()));
            Gateway.CancelSubscriptionAsync(Arg.Any<BillingCancellationRequestModel>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Snapshot));
            Cancellations.GetLatestAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => History.LastOrDefault(item => item.UserId == call.Arg<UserId>()));
            Cancellations.GetOwnedAsync(Arg.Any<BillingCancellationId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
                .Returns(call => History.SingleOrDefault(item => item.Id == call.Arg<BillingCancellationId>().Value && item.UserId == call.Arg<UserId>()));
            Cancellations.AddAsync(Arg.Any<BillingCancellation>(), Arg.Any<CancellationToken>()).Returns(call => {
                History.Add(call.Arg<BillingCancellation>());
                return Task.CompletedTask;
            });
            IBillingProviderGatewayAccessor gateways = Substitute.For<IBillingProviderGatewayAccessor>();
            gateways.GetProviderOrDefault(BillingProviderNames.Paddle).Returns(Gateway);
            _handler = new CancelSubscriptionCommandHandler(Sender, subscriptions, gateways, new NoopBillingCheckoutLock(),
                new NoOpBillingTransactionRunner(), new BillingCancellationRecorder(Cancellations, time),
                new BillingAccessService(Sender, subscriptions, time), time);
        }

        public Task<Result<BillingCancellationModel>> CancelAsync() => _handler.Handle(new CancelSubscriptionCommand(UserId.Value), CancellationToken.None);
    }
}
