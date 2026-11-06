using System.Globalization;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.ProcessTelegramStarsUpdate;
using FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;
using FoodDiary.Modules.Billing.Application.Commands.CancelSubscription;
using FoodDiary.Modules.Billing.Application.Commands.RecoverStarsRefundCancellation;
using FoodDiary.Modules.Billing.Application.Queries.GetBillingOverview;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.ValueObjects;
using FoodDiary.Modules.Users.Contracts.Commands.EnsureUserPremiumRole;
using FoodDiary.Modules.Users.Contracts.Commands.RemoveUserPremiumRole;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Users.Contracts.Queries.CheckUserAccess;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Stars_CheckoutRequiresTelegramLink_AndPersistsQuotedPriceWithoutEmail(bool linked) {
        var userId = UserId.New();
        var profile = new UserBillingProfileModel(userId, Email: null, IsActive: true, IsDeleted: false,
            HasPaidPremium: false, PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, TelegramUserId: linked ? 123 : null);
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<CheckUserAccessQuery>(), Arg.Any<CancellationToken>()).Returns((Error?)null);
        sender.Send(Arg.Any<GetUserBillingProfileQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(profile));
        IBillingProviderGateway gateway = Substitute.For<IBillingProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.TelegramStars);
        gateway.CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new BillingCheckoutSessionModel("stars_invoice", "https://t.me/$owned",
                "123:" + userId.Value.ToString("N", CultureInfo.InvariantCulture), "stars_30d_500", "monthly", 500, "XTR")));
        IBillingProviderGatewayAccessor gateways = Substitute.For<IBillingProviderGatewayAccessor>();
        gateways.GetProviderOrDefault(BillingProviderNames.TelegramStars).Returns(gateway);
        var payments = new RecordingBillingPaymentRepository();
        var handler = new CreateCheckoutSessionCommandHandler(sender, new InMemoryBillingSubscriptionRepository(), payments,
            gateways, TimeProvider.System, new NoopBillingCheckoutLock(), new NoOpBillingTransactionRunner());
        Result<BillingCheckoutSessionModel> result = await handler.Handle(
            new CreateCheckoutSessionCommand(userId.Value, "monthly", BillingProviderNames.TelegramStars), CancellationToken.None);
        Assert.Equal(linked, result.IsSuccess);
        if (linked) {
            BillingPayment order = Assert.Single(payments.Payments);
            Assert.Equal(500, order.Amount);
            Assert.Equal("XTR", order.Currency);
            await gateway.Received(1).CreateCheckoutSessionAsync(Arg.Is<BillingCheckoutSessionRequestModel>(request =>
                request.TelegramUserId == 123 && request.Email == string.Empty), Arg.Any<CancellationToken>());
        } else {
            Assert.Equal("Billing.TelegramNotLinked", result.Error.Code);
            await gateway.DidNotReceive().CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Stars_ConfirmedPaymentGrantsSharedTimedPremiumWithoutEmail_AndDuplicateChargeIsHarmless() {
        var fixture = new StarsFixture();
        ResultAssert.Success(await fixture.SendAsync(fixture.PreCheckout()));
        Assert.Equal("invoice_owned", fixture.Subscription.ExternalSubscriptionId);
        TelegramStarsUpdateModel paid = fixture.Payment();
        ResultAssert.Success(await fixture.SendAsync(paid));
        ResultAssert.Success(await fixture.SendAsync(paid));
        ResultAssert.Success(await fixture.SendAsync(paid with { UpdateId = 21 }));
        Assert.Multiple(() => {
            Assert.Equal(fixture.Now.AddDays(30), fixture.Subscription.CurrentPeriodEndUtc);
            Assert.Equal("first_charge", fixture.Subscription.ExternalPaymentMethodId);
            Assert.Equal(2, fixture.Payments.Payments.Count);
            Assert.Equal(500, fixture.Payments.Payments.Last().Amount);
            Assert.Equal("XTR", fixture.Payments.Payments.Last().Currency);
            Assert.True(fixture.Profile.HasPaidPremium);
            Assert.Equal(fixture.Now.AddDays(30), fixture.AccessEnd);
        });
    }

    [Theory]
    [InlineData(124, "XTR", 500)]
    [InlineData(123, "USD", 500)]
    [InlineData(123, "XTR", 499)]
    public async Task Stars_PreCheckoutRejectsWrongOwnerCurrencyOrAmount(long actor, string currency, int amount) {
        var fixture = new StarsFixture();
        Result<TelegramStarsUpdateResultModel> result = await fixture.SendAsync(fixture.PreCheckout() with {
            TelegramUserId = actor,
            Currency = currency,
            TotalAmount = amount,
        });
        ResultAssert.Success(result);
        Assert.Multiple(() => {
            Assert.False(result.Value.CheckoutApproved);
            Assert.Null(fixture.Subscription.ExternalSubscriptionId);
            Assert.False(fixture.Profile.HasPaidPremium);
        });
    }

    [Fact]
    public async Task Stars_StaleInvoiceOrUnlinkedAccountCannotBeApproved() {
        var fixture = new StarsFixture(stale: true);
        Assert.False((await fixture.SendAsync(fixture.PreCheckout())).Value.CheckoutApproved);
        fixture = new StarsFixture();
        fixture.Profile = fixture.Profile with { TelegramUserId = null };
        Assert.False((await fixture.SendAsync(fixture.PreCheckout())).Value.CheckoutApproved);
    }

    [Fact]
    public async Task Stars_SecondInvoiceCannotPassWhileOneInvoiceIsReserved() {
        var fixture = new StarsFixture();
        Assert.True((await fixture.SendAsync(fixture.PreCheckout())).Value.CheckoutApproved);
        Assert.True((await fixture.SendAsync(fixture.PreCheckout())).Value.CheckoutApproved);
        Assert.False((await fixture.SendAsync(fixture.PreCheckout() with { UpdateId = 11 })).Value.CheckoutApproved);
        await fixture.Payments.AddAsync(fixture.Order("invoice_other"));
        Assert.False((await fixture.SendAsync(fixture.PreCheckout() with { UpdateId = 11, InvoicePayload = "invoice_other" })).Value.CheckoutApproved);
    }

    [Fact]
    public async Task Stars_RenewalUsesProviderPeriod_OlderPaymentAndStateCannotShrinkIt() {
        var fixture = new StarsFixture();
        await fixture.SendAsync(fixture.PreCheckout());
        await fixture.SendAsync(fixture.Payment());
        await fixture.SendAsync(fixture.State(30, "canceled"));
        await fixture.SendAsync(fixture.Payment() with {
            UpdateId = 40,
            PaymentChargeId = "renewal_charge",
            IsFirstRecurring = false,
            SubscriptionExpirationUtc = fixture.Now.AddDays(60),
        });
        await fixture.SendAsync(fixture.Payment() with { UpdateId = 41, PaymentChargeId = "delayed_charge" });
        await fixture.SendAsync(fixture.State(29, "active"));
        Assert.Multiple(() => {
            Assert.Equal(fixture.Now.AddDays(60), fixture.Subscription.CurrentPeriodEndUtc);
            Assert.Equal(fixture.Now.AddDays(30), fixture.Subscription.CurrentPeriodStartUtc);
            Assert.True(fixture.Subscription.CancelAtPeriodEnd);
            Assert.Equal("first_charge", fixture.Subscription.ExternalPaymentMethodId);
            Assert.Equal(fixture.Now.AddDays(60), fixture.AccessEnd);
        });
    }

    [Fact]
    public async Task Stars_RefundOfOldPeriodPreservesRenewal_RefundOfCurrentPaymentRevokesAccess() {
        var fixture = new StarsFixture();
        await fixture.SendAsync(fixture.PreCheckout());
        await fixture.SendAsync(fixture.Payment());
        await fixture.SendAsync(fixture.Payment() with {
            UpdateId = 30,
            PaymentChargeId = "renewal_charge",
            IsFirstRecurring = false,
            SubscriptionExpirationUtc = fixture.Now.AddDays(60),
        });
        await fixture.SendAsync(fixture.Refund(40, "first_charge"));
        Assert.True(fixture.Profile.HasPaidPremium);
        Assert.Equal(fixture.Now.AddDays(60), fixture.Subscription.CurrentPeriodEndUtc);
        await fixture.SendAsync(fixture.Refund(50, "renewal_charge"));
        Assert.False(fixture.Profile.HasPaidPremium);
        Assert.Equal("canceled", fixture.Subscription.Status);
        Assert.Equal(-500, fixture.Payments.Payments.Last().Amount);
    }

    [Fact]
    public async Task Stars_RefundBeforePaymentPreventsDelayedChargeFromRestoringAccess() {
        var fixture = new StarsFixture();
        await fixture.SendAsync(fixture.PreCheckout());
        await fixture.SendAsync(fixture.Refund(30, "first_charge"));
        await fixture.SendAsync(fixture.Payment());
        Assert.False(fixture.Profile.HasPaidPremium);
    }

    [Fact]
    public async Task Stars_RefundRetriesProviderCancellationAfterCommittedRevocation() {
        var fixture = new StarsFixture(cancellationFailures: 1);
        await fixture.SendAsync(fixture.PreCheckout());
        string charge = new('x', 255);
        await fixture.SendAsync(fixture.Payment() with { PaymentChargeId = charge });
        TelegramStarsUpdateModel refund = fixture.Refund(30, charge);
        Assert.True((await fixture.SendAsync(refund)).IsFailure);
        Assert.False(fixture.Profile.HasPaidPremium);
        Assert.Equal("refunded", fixture.Subscription.Status);
        ResultAssert.Success(await fixture.SendAsync(refund));
        Assert.Equal("canceled", fixture.Subscription.Status);
        Assert.Null(fixture.Subscription.ProviderMetadataJson);
        Assert.Equal(3, fixture.Payments.Payments.Count);
        Assert.True(fixture.Payments.Payments.Last().ExternalPaymentId.Length <= 255);
    }

    [Fact]
    public async Task Stars_RefundCancellationContinuesForInactiveDeletedAccount() {
        var fixture = new StarsFixture();
        await fixture.SendAsync(fixture.PreCheckout());
        await fixture.SendAsync(fixture.Payment());
        fixture.Profile = fixture.Profile with { IsActive = false, IsDeleted = true };
        ResultAssert.Success(await fixture.SendAsync(fixture.Refund(30, "first_charge")));
        Assert.False(fixture.Profile.HasPaidPremium);
        Assert.Equal("canceled", fixture.Subscription.Status);
        Assert.Null(fixture.Subscription.ProviderMetadataJson);
    }

    [Fact]
    public async Task Stars_LatePaymentAfterProviderSwitchOnlyRecordsHistory() {
        var fixture = new StarsFixture();
        await fixture.SendAsync(fixture.PreCheckout());
        fixture.Subscription.UpdateCheckoutContext(BillingProviderNames.Paddle, "ctm_new", "pri_new", "monthly");
        await fixture.SendAsync(fixture.Payment());
        Assert.Equal(BillingProviderNames.Paddle, fixture.Subscription.Provider);
        Assert.False(fixture.Profile.HasPaidPremium);
        Assert.Equal(2, fixture.Payments.Payments.Count);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StarsFixture {
        public DateTime Now { get; }
        public DateTime? AccessEnd { get; private set; }
        public UserBillingProfileModel Profile { get; set; }
        public BillingSubscription Subscription { get; }
        public RecordingBillingPaymentRepository Payments { get; } = new();
        public ISender Sender { get; } = Substitute.For<ISender>();
        public IBillingProviderGateway Gateway { get; } = Substitute.For<IBillingProviderGateway>();
        public InMemoryBillingSubscriptionRepository Subscriptions { get; }
        public RecordingBillingWebhookEventRepository Events { get; }
        private readonly ProcessTelegramStarsUpdateCommandHandler _handler;

        public StarsFixture(bool stale = false, int cancellationFailures = 0,
            RecordingBillingWebhookEventRepository? events = null) {
            Now = DateTime.UtcNow.AddMinutes(stale ? 16 : 0);
            var clock = new FixedDateTimeProvider(Now);
            Profile = new UserBillingProfileModel(UserId.New(), Email: null, IsActive: true, IsDeleted: false,
                HasPaidPremium: false, PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, TelegramUserId: 123);
            Subscription = BillingSubscription.CreatePending(Profile.UserId, BillingProviderNames.TelegramStars, CustomerId, "stars_30d_500", "monthly");
            var subscriptions = new InMemoryBillingSubscriptionRepository(Subscription);
            Subscriptions = subscriptions;
            Events = events ?? new RecordingBillingWebhookEventRepository();
            Payments.Payments.Add(Order("invoice_owned"));
            ISender sender = Sender;
            sender.Send(Arg.Any<GetUserBillingProfileIncludingDeletedQuery>(), Arg.Any<CancellationToken>()).Returns(_ => Profile);
            sender.Send(Arg.Any<GetUserBillingProfileQuery>(), Arg.Any<CancellationToken>()).Returns(_ =>
                Profile.IsActive && !Profile.IsDeleted ? Result.Success(Profile) : Result.Failure<UserBillingProfileModel>(new Error("User.Inactive", "Account is inactive.")));
            sender.Send(Arg.Any<CheckUserAccessQuery>(), Arg.Any<CancellationToken>()).Returns(_ =>
                Profile.IsActive && !Profile.IsDeleted ? null : new Error("User.Inactive", "Account is inactive."));
            sender.Send(Arg.Any<EnsureUserPremiumRoleCommand>(), Arg.Any<CancellationToken>()).Returns(call => {
                AccessEnd = call.Arg<EnsureUserPremiumRoleCommand>().AccessEndsAtUtc;
                Profile = Profile with { HasPaidPremium = true };
                return Unit.Value;
            });
            sender.Send(Arg.Any<RemoveUserPremiumRoleCommand>(), Arg.Any<CancellationToken>()).Returns(_ => {
                Profile = Profile with { HasPaidPremium = false };
                return Unit.Value;
            });
            IBillingPublicConfigProvider config = Substitute.For<IBillingPublicConfigProvider>();
            config.GetPublicConfig().Returns(new BillingPublicConfigModel(BillingProviderNames.Paddle, PaddleClientToken: null,
                [BillingProviderNames.Paddle, BillingProviderNames.TelegramStars], TelegramStarsPrice: 500));
            var access = new BillingAccessService(sender, subscriptions, clock);
            var recorder = new BillingCancellationRecorder(Substitute.For<IBillingCancellationRepository>(), clock);
            IBillingProviderGateway gateway = Gateway;
            gateway.CancelSubscriptionAsync(Arg.Any<BillingCancellationRequestModel>(), Arg.Any<CancellationToken>()).Returns(call => {
                if (cancellationFailures-- > 0) {
                    return Result.Failure<BillingCancellationSnapshotModel>(BillingErrors.ProviderOperationFailed(BillingProviderNames.TelegramStars, "Temporary provider failure"));
                }
                BillingCancellationRequestModel request = call.Arg<BillingCancellationRequestModel>();
                DateTime end = request.CurrentPeriodEndUtc ?? Now;
                return Result.Success(new BillingCancellationSnapshotModel("active", request.CurrentPeriodStartUtc, end,
                    CancelAtPeriodEnd: true, CanceledAtUtc: null, end, Now));
            });
            IBillingProviderGatewayAccessor gateways = Substitute.For<IBillingProviderGatewayAccessor>();
            gateways.GetProviderOrDefault(BillingProviderNames.TelegramStars).Returns(gateway);
            var cancel = new CancelSubscriptionCommandHandler(sender, subscriptions, gateways, new NoopBillingCheckoutLock(),
                new NoOpBillingTransactionRunner(), recorder, access, clock);
            sender.Send(Arg.Any<CancelSubscriptionCommand>(), Arg.Any<CancellationToken>())
                .Returns(call => cancel.Handle(call.Arg<CancelSubscriptionCommand>(), call.Arg<CancellationToken>()));
            var recovery = new RecoverStarsRefundCancellationCommandHandler(subscriptions, gateways, new NoopBillingCheckoutLock(),
                new NoOpBillingTransactionRunner(), access, recorder, clock);
            sender.Send(Arg.Any<RecoverStarsRefundCancellationCommand>(), Arg.Any<CancellationToken>())
                .Returns(call => recovery.Handle(call.Arg<RecoverStarsRefundCancellationCommand>(), call.Arg<CancellationToken>()));
            var overview = new GetBillingOverviewQueryHandler(sender, subscriptions, config, clock);
            sender.Send(Arg.Any<GetBillingOverviewQuery>(), Arg.Any<CancellationToken>())
                .Returns(call => overview.Handle(call.Arg<GetBillingOverviewQuery>(), call.Arg<CancellationToken>()));
            _handler = new ProcessTelegramStarsUpdateCommandHandler(Payments, subscriptions, Events,
                new NoOpBillingTransactionRunner(), sender, access, config, recorder, clock);
        }

        public BillingPayment Order(string payload) => BillingPayment.CreateWithFinancials(Profile.UserId, Subscription.TypedId,
            BillingProviderNames.TelegramStars, payload, CustomerId, externalSubscriptionId: null, externalPaymentMethodId: null,
            "stars_30d_500", "monthly", BillingSubscription.PendingCheckoutStatus, BillingPaymentKinds.Checkout,
            BillingPaymentFinancials.FromFields(500, "XTR"), currentPeriodStartUtc: null, currentPeriodEndUtc: null,
            webhookEventId: null, providerMetadataJson: null);

        private string CustomerId => "123:" + Profile.UserId.Value.ToString("N", CultureInfo.InvariantCulture);

        public TelegramStarsUpdateModel PreCheckout() => new(10, 123, "pre_checkout", "invoice_owned", "XTR", 500);
        public TelegramStarsUpdateModel Payment() => new(20, 123, "payment", "invoice_owned", "XTR", 500, "first_charge",
            Now.AddDays(30), Now, IsRecurring: true, IsFirstRecurring: true);
        public TelegramStarsUpdateModel Refund(long id, string charge) => new(id, 123, "refund", "invoice_owned", "XTR", 500,
            charge, PaymentOccurredAtUtc: Now);
        public TelegramStarsUpdateModel State(long id, string state) => new(id, 123, "subscription", "invoice_owned", SubscriptionState: state);
        public Task<Result<TelegramStarsUpdateResultModel>> SendAsync(TelegramStarsUpdateModel update) =>
            _handler.Handle(new ProcessTelegramStarsUpdateCommand(update), CancellationToken.None);
    }
}
