using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.SendCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Commands.SendCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Models;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Identity.Contracts.Email.Commands.QueueSubscriptionCancellationEmail;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Fact]
    public async Task CancellationEmail_QueuesOnceAndKeepsCancellationAndFeedbackUnchanged() {
        var fixture = new CancellationEmailFixture();
        BillingCancellation cancellation = fixture.Rows[0];
        cancellation.SubmitFeedback("too_expensive", "Private feedback", Now);

        BillingCancellationEmailRunResult first = await fixture.RunAsync();
        BillingCancellationEmailRunResult second = await fixture.RunAsync();

        Assert.Equal(new BillingCancellationEmailRunResult(1, 0, 0), first);
        Assert.Equal(new BillingCancellationEmailRunResult(0, 1, 0), second);
        Assert.Equal("scheduled", cancellation.Outcome);
        Assert.Equal("Private feedback", cancellation.Comment);
        Assert.Equal(Now, cancellation.ConfirmationEmailHandledAtUtc);
        await fixture.Sender.Received(1).Send(Arg.Is<QueueSubscriptionCancellationEmailCommand>(command =>
            command.Language == "ru" && command.TimeZoneId == "Asia/Tbilisi" && command.AccessUntilUtc == Now.AddDays(30)), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, false, true, "recipient@example.test")]
    [InlineData(true, true, true, "recipient@example.test")]
    [InlineData(true, false, false, "recipient@example.test")]
    [InlineData(true, false, true, null)]
    public async Task CancellationEmail_UnsafeOrMissingRecipient_IsSkipped(bool active, bool deleted, bool confirmed, string? email) {
        var fixture = new CancellationEmailFixture();
        fixture.Sender.Send(Arg.Any<GetUserBillingProfileIncludingDeletedQuery>(), Arg.Any<CancellationToken>())
            .Returns(new UserBillingProfileModel(fixture.Rows[0].UserId, email, active, deleted, HasPaidPremium: true,
                PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, IsEmailConfirmed: confirmed));
        BillingCancellationEmailRunResult result = await fixture.RunAsync();
        Assert.Equal(new BillingCancellationEmailRunResult(0, 1, 0), result);
        Assert.NotNull(fixture.Rows[0].ConfirmationEmailHandledAtUtc);
        await fixture.Sender.DidNotReceive().Send(Arg.Any<QueueSubscriptionCancellationEmailCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancellationEmail_RemovedCancellation_DoesNotSendStaleConfirmation() {
        var fixture = new CancellationEmailFixture();
        fixture.Subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_email", externalPaymentMethodId: null, "price", "monthly", "active",
            Now, Now.AddDays(30), cancelAtPeriodEnd: false, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null,
            "evt_resume", Now, webhookOccurredAtUtc: Now);
        fixture.Rows[0].Synchronize(fixture.Subscription, Now);
        Assert.Equal("cancellation_removed", fixture.Rows[0].Outcome);
        Assert.Equal(new BillingCancellationEmailRunResult(0, 1, 0), await fixture.RunAsync());
        await fixture.Sender.DidNotReceive().Send(Arg.Any<QueueSubscriptionCancellationEmailCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancellationEmail_QueueFailure_RetainsIntentAndAllowsLaterCandidatesAndRetry() {
        var fixture = new CancellationEmailFixture();
        BillingCancellation first = fixture.Rows[0];
        BillingCancellation second = CancellationEmailFixture.CreateCancellation(UserId.New());
        fixture.Rows.Add(second);
        fixture.Sender.Send(Arg.Any<QueueSubscriptionCancellationEmailCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<QueueSubscriptionCancellationEmailCommand>().CorrelationId.Contains(first.Id.ToString("N"), StringComparison.Ordinal)
                ? throw new InvalidOperationException("Synthetic queue failure") : Result.Success());

        Assert.Equal(new BillingCancellationEmailRunResult(1, 0, 1), await fixture.RunAsync());
        Assert.Null(first.ConfirmationEmailHandledAtUtc);
        Assert.NotNull(second.ConfirmationEmailHandledAtUtc);
        Assert.Equal("scheduled", first.Outcome);
        fixture.Sender.Send(Arg.Any<QueueSubscriptionCancellationEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        Assert.Equal(new BillingCancellationEmailRunResult(1, 1, 0), await fixture.RunAsync());
        Assert.NotNull(first.ConfirmationEmailHandledAtUtc);
    }

    [Fact]
    public async Task CancellationEmail_ImmediateOrEndedAccess_DoesNotPromiseFutureAccess() {
        var fixture = new CancellationEmailFixture();
        fixture.Subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_email", externalPaymentMethodId: null, "price", "monthly", "canceled",
            currentPeriodStartUtc: null, currentPeriodEndUtc: null, cancelAtPeriodEnd: false, canceledAtUtc: Now, trialStartUtc: null, trialEndUtc: null,
            "evt_canceled", Now, webhookOccurredAtUtc: Now);
        fixture.Rows[0].Synchronize(fixture.Subscription, Now);
        await fixture.RunAsync();
        await fixture.Sender.Received(1).Send(Arg.Is<QueueSubscriptionCancellationEmailCommand>(command => command.AccessUntilUtc == null), Arg.Any<CancellationToken>());
    }

    private sealed class CancellationEmailFixture {
        public ISender Sender { get; } = Substitute.For<ISender>();
        public List<BillingCancellation> Rows { get; } = [];
        public BillingSubscription Subscription { get; }
        private readonly SendCancellationConfirmationsCommandHandler _handler;

        public CancellationEmailFixture() {
            var userId = UserId.New();
            Subscription = CreateSubscription(userId);
            Rows.Add(BillingCancellation.Create(Subscription, "fooddiary", Now, Now.AddDays(30)));
            IBillingCancellationRepository repository = Substitute.For<IBillingCancellationRepository>();
            repository.GetPendingConfirmationsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(_ => Rows.Select(row => new BillingCancellationEmailCandidateModel(row.TypedId, row.UserId)).ToArray());
            repository.GetOwnedAsync(Arg.Any<BillingCancellationId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
                .Returns(call => Rows.SingleOrDefault(row => row.TypedId == call.Arg<BillingCancellationId>() && row.UserId == call.Arg<UserId>()));
            Sender.Send(Arg.Any<GetUserBillingProfileIncludingDeletedQuery>(), Arg.Any<CancellationToken>())
                .Returns(call => new UserBillingProfileModel(call.Arg<GetUserBillingProfileIncludingDeletedQuery>().UserId,
                    "recipient@example.test", IsActive: true, IsDeleted: false, HasPaidPremium: true,
                    PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, IsEmailConfirmed: true, Language: "ru", TimeZoneId: "Asia/Tbilisi"));
            Sender.Send(Arg.Any<QueueSubscriptionCancellationEmailCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
            _handler = new SendCancellationConfirmationsCommandHandler(repository, new NoOpBillingTransactionRunner(), Sender,
                new FixedDateTimeProvider(Now), NullLogger<SendCancellationConfirmationsCommandHandler>.Instance);
        }

        public Task<BillingCancellationEmailRunResult> RunAsync() => _handler.Handle(new SendCancellationConfirmationsCommand(100), CancellationToken.None);
        public static BillingCancellation CreateCancellation(UserId userId) => BillingCancellation.Create(CreateSubscription(userId), "fooddiary", Now, Now.AddDays(30));
        private static BillingSubscription CreateSubscription(UserId userId) {
            var subscription = BillingSubscription.CreatePending(userId, BillingProviderNames.Paddle, "ctm_email", "price", "monthly");
            subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_email", externalPaymentMethodId: null, "price", "monthly", "active", Now,
                Now.AddDays(30), cancelAtPeriodEnd: true, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null,
                "evt_cancel", Now, webhookOccurredAtUtc: Now);
            return subscription;
        }
    }
}
