using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.CancelSubscription;
using FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;
using FoodDiary.Modules.Billing.Application.Commands.RecoverStarsRefundCancellation;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Theory]
    [InlineData("negative-id")]
    [InlineData("missing-actor")]
    [InlineData("empty-invoice")]
    [InlineData("long-invoice")]
    [InlineData("unknown-kind")]
    [InlineData("unknown-invoice")]
    [InlineData("wrong-owner")]
    [InlineData("wrong-currency")]
    [InlineData("wrong-amount")]
    [InlineData("missing-charge")]
    [InlineData("long-charge")]
    [InlineData("non-utc-payment")]
    [InlineData("not-recurring")]
    [InlineData("missing-expiration")]
    [InlineData("early-expiration")]
    public async Task Stars_InvalidPaymentDoesNotGrantAccessOrPersistFinancialEvents(string scenario) {
        var fixture = new StarsFixture();
        TelegramStarsUpdateModel payment = fixture.Payment();
        payment = scenario switch {
            "negative-id" => payment with { UpdateId = -1 },
            "missing-actor" => payment with { TelegramUserId = 0 },
            "empty-invoice" => payment with { InvoicePayload = "" },
            "long-invoice" => payment with { InvoicePayload = new string('x', 129) },
            "unknown-kind" => payment with { Kind = "unknown" },
            "unknown-invoice" => payment with { InvoicePayload = "unknown" },
            "wrong-owner" => payment with { TelegramUserId = 999 },
            "wrong-currency" => payment with { Currency = "USD" },
            "wrong-amount" => payment with { TotalAmount = 499 },
            "missing-charge" => payment with { PaymentChargeId = " " },
            "long-charge" => payment with { PaymentChargeId = new string('x', 256) },
            "non-utc-payment" => payment with { PaymentOccurredAtUtc = DateTime.SpecifyKind(fixture.Now, DateTimeKind.Unspecified) },
            "not-recurring" => payment with { IsRecurring = false },
            "missing-expiration" => payment with { SubscriptionExpirationUtc = null },
            "early-expiration" => payment with { SubscriptionExpirationUtc = fixture.Now },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        ResultAssert.Failure(await fixture.SendAsync(payment), "Billing.WebhookValidationFailed");

        Assert.Multiple(() => {
            Assert.Single(fixture.Payments.Payments);
            Assert.Empty(fixture.Events.Events);
            Assert.False(fixture.Profile.HasPaidPremium);
            Assert.Equal(BillingSubscription.PendingCheckoutStatus, fixture.Subscription.Status);
        });
    }

    [Fact]
    public async Task Stars_UnknownPreCheckoutIsRejectedWithoutCreatingASubscription() {
        var fixture = new StarsFixture();

        Result<TelegramStarsUpdateResultModel> result = await fixture.SendAsync(fixture.PreCheckout() with { InvoicePayload = "unknown" });

        ResultAssert.Success(result);
        Assert.Multiple(() => {
            Assert.False(result.Value.CheckoutApproved);
            Assert.NotNull(result.Value.Error);
            Assert.Null(fixture.Subscription.ExternalSubscriptionId);
        });
    }

    [Fact]
    public async Task Stars_UnknownStatePreservesConfirmedPaidPeriod() {
        var fixture = new StarsFixture();
        ResultAssert.Success(await fixture.SendAsync(fixture.PreCheckout()));
        ResultAssert.Success(await fixture.SendAsync(fixture.Payment()));

        ResultAssert.Failure(await fixture.SendAsync(fixture.State(30, "unknown")), "Billing.WebhookValidationFailed");

        Assert.Multiple(() => {
            Assert.Equal("active", fixture.Subscription.Status);
            Assert.Equal(fixture.Now.AddDays(30), fixture.AccessEnd);
            Assert.Single(fixture.Events.Events);
        });
    }

    [Fact]
    public async Task Stars_DuplicateReceiptConflictIsHarmlessOnRedelivery() {
        var fixture = new StarsFixture(events: new RecordingBillingWebhookEventRepository { ThrowAlreadyProcessedOnAdd = true });
        ResultAssert.Success(await fixture.SendAsync(fixture.PreCheckout()));

        ResultAssert.Success(await fixture.SendAsync(fixture.Payment()));
        ResultAssert.Success(await fixture.SendAsync(fixture.Payment()));

        Assert.Multiple(() => {
            Assert.Equal(2, fixture.Payments.Payments.Count);
            Assert.True(fixture.Profile.HasPaidPremium);
            Assert.Equal(fixture.Now.AddDays(30), fixture.AccessEnd);
        });
    }

    [Theory]
    [InlineData("refund-pending", "Billing.CheckoutAlreadyInProgress")]
    [InlineData("recurring-active", "Billing.SubscriptionAlreadyActive")]
    public async Task Stars_CheckoutCannotReplaceAnUnresolvedRecurringSubscription(string scenario, string error) {
        var fixture = new StarsFixture(stale: true);
        SetStarsSnapshot(fixture, scenario is "refund-pending" ? "refunded" : "active",
            scenario is "refund-pending" ? TelegramStarsBillingState.RefundCancellationPending : null);
        CreateCheckoutSessionCommandHandler handler = StarsCheckoutHandler(fixture);

        ResultAssert.Failure(await handler.Handle(new CreateCheckoutSessionCommand(fixture.Profile.UserId.Value, "monthly",
            BillingProviderNames.TelegramStars), CancellationToken.None), error);

        await fixture.Gateway.DidNotReceive().CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>());
        Assert.Single(fixture.Payments.Payments);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(999L)]
    public async Task Stars_CheckoutRechecksTelegramIdentityAfterProviderHttp(long? newActor) {
        var fixture = new StarsFixture(stale: true);
        CreateCheckoutSessionCommandHandler handler = StarsCheckoutHandler(fixture);
        fixture.Gateway.CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                fixture.Profile = fixture.Profile with { TelegramUserId = newActor };
                return Result.Success(new BillingCheckoutSessionModel("new-invoice", "https://t.me/$owned", "123:owned", "stars_30d_500", "monthly", 500, "XTR"));
            });

        ResultAssert.Failure(await handler.Handle(new CreateCheckoutSessionCommand(fixture.Profile.UserId.Value, "monthly",
            BillingProviderNames.TelegramStars), CancellationToken.None), "Billing.TelegramNotLinked");

        Assert.Multiple(() => {
            Assert.Single(fixture.Payments.Payments);
            Assert.Null(fixture.Subscription.ExternalSubscriptionId);
        });
    }

    [Fact]
    public async Task Stars_CheckoutDoesNotOverwriteSubscriptionChangedDuringProviderHttp() {
        var fixture = new StarsFixture(stale: true);
        CreateCheckoutSessionCommandHandler handler = StarsCheckoutHandler(fixture);
        fixture.Gateway.CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                fixture.Subscription.UpdateCheckoutContext(BillingProviderNames.TelegramStars, "customer-replaced", "price-replaced", "monthly");
                return Result.Success(new BillingCheckoutSessionModel("new-invoice", "https://t.me/$owned", "123:owned", "stars_30d_500", "monthly", 500, "XTR"));
            });

        ResultAssert.Failure(await handler.Handle(new CreateCheckoutSessionCommand(fixture.Profile.UserId.Value, "monthly",
            BillingProviderNames.TelegramStars), CancellationToken.None), "Billing.CheckoutAlreadyInProgress");

        Assert.Multiple(() => {
            Assert.Single(fixture.Payments.Payments);
            Assert.Null(fixture.Subscription.ExternalSubscriptionId);
            Assert.Equal(0, fixture.Subscriptions.UpdateCount);
            Assert.Equal("customer-replaced", fixture.Subscription.ExternalCustomerId);
            Assert.Equal("price-replaced", fixture.Subscription.ExternalPriceId);
        });
        await fixture.Gateway.Received(1).CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stars_RefundedCancellationRevokesAccessAndClearsPendingRecovery() {
        var fixture = new StarsFixture();
        SetStarsSnapshot(fixture, "refunded", TelegramStarsBillingState.RefundCancellationPending);

        Result<BillingCancellationModel> result = await fixture.Sender.Send(new CancelSubscriptionCommand(fixture.Profile.UserId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(() => {
            Assert.Equal("canceled", fixture.Subscription.Status);
            Assert.True(fixture.Subscription.CancelAtPeriodEnd);
            Assert.Equal(fixture.Now, fixture.Subscription.CanceledAtUtc);
            Assert.Null(fixture.Subscription.ProviderMetadataJson);
            Assert.False(fixture.Profile.HasPaidPremium);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stars_RecoveryWithoutIntentIsHarmless_AndPendingRecoveryNeedsAGateway(bool pending) {
        var fixture = new StarsFixture();
        if (pending) { SetStarsSnapshot(fixture, "refunded", TelegramStarsBillingState.RefundCancellationPending); }
        IBillingProviderGatewayAccessor gateways = Substitute.For<IBillingProviderGatewayAccessor>();
        gateways.GetProviderOrDefault(BillingProviderNames.TelegramStars).Returns((IBillingProviderGateway?)null);

        Result<Unit> result = await StarsRecoveryHandler(fixture, gateways).Handle(
            new RecoverStarsRefundCancellationCommand(fixture.Profile.UserId), CancellationToken.None);

        if (pending) { ResultAssert.Failure(result, "Billing.CancellationUnavailable"); } else { ResultAssert.Success(result); }
        Assert.Equal(pending ? "refunded" : BillingSubscription.PendingCheckoutStatus, fixture.Subscription.Status);
        Assert.Equal(0, fixture.Subscriptions.UpdateCount);
    }

    [Fact]
    public async Task Stars_RecoveryDoesNotOverwriteASubscriptionRemovedDuringHttp() {
        var fixture = new StarsFixture();
        SetStarsSnapshot(fixture, "refunded", TelegramStarsBillingState.RefundCancellationPending);
        IBillingProviderGatewayAccessor gateways = Substitute.For<IBillingProviderGatewayAccessor>();
        gateways.GetProviderOrDefault(BillingProviderNames.TelegramStars).Returns(fixture.Gateway);
        fixture.Gateway.CancelSubscriptionAsync(Arg.Any<BillingCancellationRequestModel>(), Arg.Any<CancellationToken>()).Returns(_ => {
            fixture.Subscriptions.Subscriptions.Clear();
            return Result.Success(new BillingCancellationSnapshotModel("active", fixture.Now, fixture.Now.AddDays(30),
                CancelAtPeriodEnd: true, CanceledAtUtc: null, fixture.Now, fixture.Now));
        });

        ResultAssert.Success(await StarsRecoveryHandler(fixture, gateways).Handle(
            new RecoverStarsRefundCancellationCommand(fixture.Profile.UserId), CancellationToken.None));

        Assert.Multiple(() => {
            Assert.Empty(fixture.Subscriptions.Subscriptions);
            Assert.Equal(0, fixture.Subscriptions.UpdateCount);
            Assert.Equal("refunded", fixture.Subscription.Status);
        });
    }

    private static void SetStarsSnapshot(StarsFixture fixture, string status, string? metadata) =>
        fixture.Subscription.ApplyProviderSnapshot(BillingProviderNames.TelegramStars, "invoice_owned", "first_charge", "stars_30d_500", "monthly",
            status, fixture.Now.AddDays(-30), fixture.Now.AddTicks(-1), cancelAtPeriodEnd: false, canceledAtUtc: null,
            trialStartUtc: null, trialEndUtc: null, "stars-update:1", fixture.Now,
            metadata, webhookOccurredAtUtc: fixture.Now);

    private static CreateCheckoutSessionCommandHandler StarsCheckoutHandler(StarsFixture fixture) {
        fixture.Gateway.Provider.Returns(BillingProviderNames.TelegramStars);
        IBillingProviderGatewayAccessor gateways = Substitute.For<IBillingProviderGatewayAccessor>();
        gateways.GetProviderOrDefault(BillingProviderNames.TelegramStars).Returns(fixture.Gateway);
        return new CreateCheckoutSessionCommandHandler(fixture.Sender, fixture.Subscriptions, fixture.Payments, gateways,
            new FixedDateTimeProvider(fixture.Now), new NoopBillingCheckoutLock(), new NoOpBillingTransactionRunner());
    }

    private static RecoverStarsRefundCancellationCommandHandler StarsRecoveryHandler(StarsFixture fixture, IBillingProviderGatewayAccessor gateways) {
        var clock = new FixedDateTimeProvider(fixture.Now);
        return new RecoverStarsRefundCancellationCommandHandler(fixture.Subscriptions, gateways, new NoopBillingCheckoutLock(),
            new NoOpBillingTransactionRunner(), new BillingAccessService(fixture.Sender, fixture.Subscriptions, clock),
            new BillingCancellationRecorder(Substitute.For<IBillingCancellationRepository>(), clock), clock);
    }
}
