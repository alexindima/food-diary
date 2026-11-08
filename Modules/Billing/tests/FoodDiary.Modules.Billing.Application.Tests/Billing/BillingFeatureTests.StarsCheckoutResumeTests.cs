using System.Globalization;
using System.Text.Json;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.ValueObjects;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.CheckUserAccess;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Tests.Billing;

public partial class BillingFeatureTests {
    [Fact]
    public async Task StarsCheckout_CanceledInvoiceReopensAcrossHandlersWithoutAnotherProviderCall() {
        var fixture = new StarsResumeFixture();
        BillingCheckoutSessionModel first = ResultAssert.Success(await fixture.AttemptAsync("first"));
        BillingPayment order = Assert.Single(fixture.Payments.Payments);
        DateTime created = order.CreatedOnUtc;
        DateTime? modified = fixture.Subscription.ModifiedOnUtc;
        fixture.Gateway.CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(first with { SessionId = "new_invoice", Url = "https://t.me/$new", Amount = 700 }));

        BillingCheckoutSessionModel retry = ResultAssert.Success(await fixture.AttemptAsync("after_cancel"));
        Assert.Multiple(() => {
            Assert.Equal(first, retry);
            Assert.Single(fixture.Payments.Payments);
            Assert.Equal(created, order.CreatedOnUtc);
            Assert.Equal(modified, fixture.Subscription.ModifiedOnUtc);
        });
        await fixture.Gateway.Received(1).CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StarsCheckout_SavesInvoiceWithTheOriginalQuote() {
        var fixture = new StarsResumeFixture();
        BillingCheckoutSessionModel first = ResultAssert.Success(await fixture.AttemptAsync("first"));
        BillingPayment order = Assert.Single(fixture.Payments.Payments);
        using var document = JsonDocument.Parse(Assert.IsType<string>(order.ProviderMetadataJson));
        Assert.Multiple(() => {
            Assert.Equal(first.Url, document.RootElement.GetProperty("invoiceUrl").GetString());
            Assert.Equal(300, document.RootElement.GetProperty("subscriptionPeriodSeconds").GetInt32());
        });
    }

    [Theory]
    [InlineData("payer")]
    [InlineData("reserved")]
    [InlineData("legacy")]
    [InlineData("foreign_url")]
    [InlineData("not_invoice")]
    public async Task StarsCheckout_UnsafeOrReservedInvoiceCannotBeResumed(string reason) {
        var fixture = new StarsResumeFixture();
        BillingCheckoutSessionModel first = ResultAssert.Success(await fixture.AttemptAsync("first"));
        if (reason is "payer") {
            fixture.Profile = fixture.Profile with { TelegramUserId = 124 };
        } else if (reason is "reserved") {
            fixture.Subscription.ApplyProviderSnapshot(BillingProviderNames.TelegramStars, first.SessionId, externalPaymentMethodId: null, first.PriceId,
                first.Plan, BillingSubscription.PendingCheckoutStatus, currentPeriodStartUtc: null, currentPeriodEndUtc: null,
                cancelAtPeriodEnd: false, canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null, "pre_checkout", DateTime.UtcNow);
        } else {
            string metadata = reason switch {
                "legacy" => "{\"subscriptionPeriodSeconds\":300}",
                "foreign_url" => "{\"subscriptionPeriodSeconds\":300,\"invoiceUrl\":\"https://t.me.evil.example/$owned\"}",
                _ => "{\"subscriptionPeriodSeconds\":300,\"invoiceUrl\":\"https://t.me/owned\"}",
            };
            BillingPayment order = Assert.Single(fixture.Payments.Payments);
            order.ApplyProviderObservation(order.SubscriptionReference, order.ExternalCustomerId, externalSubscriptionId: null, externalPaymentMethodId: null, order.ExternalPriceId,
                order.Plan, order.Status, order.Kind, BillingPaymentFinancials.FromFields(order.Amount, order.Currency),
                currentPeriodStartUtc: null, currentPeriodEndUtc: null, webhookEventId: null, metadata);
        }
        Result<BillingCheckoutSessionModel> result = await fixture.AttemptAsync("retry");
        ResultAssert.Failure(result);
        Assert.Equal("Billing.CheckoutAlreadyInProgress", result.Error.Code);
        await fixture.Gateway.Received(1).CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StarsCheckout_ExpiredInvoiceUsesNormalCreationInsteadOfExtendingTheOldOrder() {
        var fixture = new StarsResumeFixture();
        BillingCheckoutSessionModel first = ResultAssert.Success(await fixture.AttemptAsync("first"));
        fixture.Gateway.CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(first with { SessionId = "fresh_invoice", Url = "https://t.me/$fresh" }));
        BillingCheckoutSessionModel next = ResultAssert.Success(await fixture.AttemptAsync("later",
            new FixedDateTimeProvider(DateTime.UtcNow.AddMinutes(16))));
        Assert.Equal("fresh_invoice", next.SessionId);
        Assert.Equal(2, fixture.Payments.Payments.Count);
        await fixture.Gateway.Received(2).CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StarsCheckout_PaddleRetainsItsCheckoutCooldown() {
        var fixture = new StarsResumeFixture(BillingProviderNames.Paddle);
        ResultAssert.Success(await fixture.AttemptAsync("first"));
        Result<BillingCheckoutSessionModel> result = await fixture.AttemptAsync("retry");
        ResultAssert.Failure(result);
        Assert.Equal("Billing.CheckoutAlreadyInProgress", result.Error.Code);
        await fixture.Gateway.Received(1).CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class StarsResumeFixture {
        public UserBillingProfileModel Profile { get; set; } = new(UserId.New(), "owned@example.com", IsActive: true, IsDeleted: false, HasPaidPremium: false,
            PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, IsEmailConfirmed: true, TelegramUserId: 123);
        public RecordingBillingPaymentRepository Payments { get; } = new();
        public InMemoryBillingSubscriptionRepository Subscriptions { get; } = new();
        public BillingSubscription Subscription => Assert.Single(Subscriptions.Subscriptions);
        public IBillingProviderGateway Gateway { get; } = Substitute.For<IBillingProviderGateway>();
        private readonly ISender _sender = Substitute.For<ISender>();
        private readonly IBillingProviderGatewayAccessor _gateways = Substitute.For<IBillingProviderGatewayAccessor>();
        private readonly string _provider;

        public StarsResumeFixture(string provider = BillingProviderNames.TelegramStars) {
            _provider = provider;
            _sender.Send(Arg.Any<CheckUserAccessQuery>(), Arg.Any<CancellationToken>()).Returns((Error?)null);
            _sender.Send(Arg.Any<GetUserBillingProfileQuery>(), Arg.Any<CancellationToken>()).Returns(_ => Result.Success(Profile));
            Gateway.Provider.Returns(provider);
            Gateway.CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success(new BillingCheckoutSessionModel("invoice_owned", "https://t.me/$owned",
                    "123:" + Profile.UserId.Value.ToString("N", CultureInfo.InvariantCulture), "stars_test_5m_500", "monthly", 500, "XTR", 300)));
            _gateways.GetProviderOrDefault(provider).Returns(Gateway);
        }

        public Task<Result<BillingCheckoutSessionModel>> AttemptAsync(string key, TimeProvider? clock = null) {
            var handler = new CreateCheckoutSessionCommandHandler(_sender, Subscriptions, Payments, _gateways,
                clock ?? TimeProvider.System, new NoopBillingCheckoutLock(), new NoOpBillingTransactionRunner());
            return handler.Handle(new CreateCheckoutSessionCommand(Profile.UserId.Value, "monthly", _provider, key), CancellationToken.None);
        }
    }
}
